// Assets/Script/CutScene/CutsceneRouter.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CutsceneRouter : MonoBehaviour
{
    private static int s_runningCutsceneCount = 0;
    public static bool IsAnyCutsceneRunning => s_runningCutsceneCount > 0;

    [Serializable]
    public class Route
    {
        public string key = "CutScene1";
        public List<TriggerStepBase> steps = new List<TriggerStepBase>();
    }

    [Header("Routes (Key -> Steps)")]
    [SerializeField] private List<Route> routes = new List<Route>();

    [Header("Auto Start")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private string startKey = "CutScene1";
    [Tooltip("Start에서 1프레임 기다린 뒤 실행(다른 매니저 Start 타이밍 보정)")]
    [SerializeField] private bool delayOneFrame = true;
    [Tooltip("Auto Start key를 저장 플래그로 1회만 실행")]
    [SerializeField] private bool oneShotStartKey = true;

    [Header("Policy")]
    [Tooltip("같은 Key를 다시 실행 허용할지")]
    [SerializeField] private bool allowReentrySameKey = false;
    [Tooltip("실행 중 다른 실행 요청이 오면 무시")]
    [SerializeField] private bool ignoreWhenRunning = true;

    [Header("Input Lock (선택)")]
    [SerializeField] private bool lockActionViaGameManager = true;
    [SerializeField] private bool disablePlayerMoveComponent = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool stopOnStepError = true;

    private bool _running = false;
    private readonly HashSet<string> _playedKeys = new HashSet<string>();

    // lock runtime
    private bool _heldActionLock = false;
    private PlayerMove _pmCached = null;
    private bool _pmWasEnabled = false;
    private string _resolvedStartKey = null;

    private void Start()
    {
        if (!playOnStart) return;
        StartCoroutine(Co_AutoStart());
    }

    private IEnumerator Co_AutoStart()
    {
        if (delayOneFrame) yield return null;

        string autoStartKey = ResolveAutoStartKey();
        if (string.IsNullOrWhiteSpace(autoStartKey))
        {
            if (debugLog) Debug.LogWarning("[CutsceneRouter] AutoStart skipped because resolved start key is empty");
            yield break;
        }

        if (oneShotStartKey && IsStartKeyAlreadyConsumed(autoStartKey))
        {
            if (debugLog) Debug.Log($"[CutsceneRouter] skip AutoStart('{autoStartKey}') (already consumed)");
            yield break;
        }

        yield return Co_Play(autoStartKey);

        if (oneShotStartKey && _playedKeys.Contains(autoStartKey))
            ConsumeStartKey(autoStartKey);
    }

    /// <summary>
    /// 외부에서 key로 컷씬 실행
    /// </summary>
    public void Play(string key)
    {
        StartCoroutine(Co_Play(key));
    }

    private IEnumerator Co_Play(string key)
    {
        if (string.IsNullOrEmpty(key)) yield break;

        if (_running && ignoreWhenRunning)
        {
            if (debugLog) Debug.LogWarning($"[CutsceneRouter] ignore Play('{key}') because running");
            yield break;
        }

        if (!allowReentrySameKey && _playedKeys.Contains(key))
        {
            if (debugLog) Debug.Log($"[CutsceneRouter] skip Play('{key}') (already played)");
            yield break;
        }

        var route = FindRoute(key);
        if (route == null)
        {
            Debug.LogWarning($"[CutsceneRouter] route not found: '{key}'");
            yield break;
        }

        _running = true;
        s_runningCutsceneCount++;
        _playedKeys.Add(key);

        BeginInputLock();

        // -----------------------------
        //  TriggerContext는 "필수 생성자"로 만들어야 함
        // TriggerContext(TriggerGet trigger, TriggerRouter router, GameObject actor, Collider2D hit, PlayerMove playerMove)
        // 여기서는 trigger/router가 없으니 null로 넣고,
        // actor는 Player(=playerMove.gameObject)로 넣어서 ctx.player가 잡히게 함.
        // -----------------------------
        PlayerMove pm = ResolvePlayerMove();
        GameObject actor = (pm != null) ? pm.gameObject : null;
        Collider2D hit = (actor != null) ? actor.GetComponent<Collider2D>() : null;

        TriggerContext ctx = new TriggerContext(
            null,   // TriggerGet (없으니 null)
            null,   // TriggerRouter (없으니 null)
            actor,  // actor(플레이어)
            hit,    // hit(플레이어 콜라이더, 없으면 null)
            pm      // playerMove
        );

        if (debugLog) Debug.Log($"[CutsceneRouter] START key='{key}' steps={route.steps.Count}");

        for (int i = 0; i < route.steps.Count; i++)
        {
            var step = route.steps[i];
            if (step == null)
            {
                if (debugLog) Debug.LogWarning($"[CutsceneRouter] step[{i}] is null -> skip");
                continue;
            }

            if (debugLog) Debug.Log($"[CutsceneRouter] step[{i}] -> {step.name} ({step.GetType().Name})");

            IEnumerator it = null;
            try
            {
                it = step.Execute(ctx);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CutsceneRouter] step[{i}] Execute() threw: {e}");
                if (stopOnStepError) break;
                else continue;
            }

            if (it != null)
            {
                bool stepError = false;
                while (true)
                {
                    object cur = null;
                    try
                    {
                        if (!it.MoveNext()) break;
                        cur = it.Current;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CutsceneRouter] step[{i}] coroutine error: {e}");
                        stepError = true;
                        break;
                    }

                    yield return cur;
                }

                if (stepError && stopOnStepError) break;
            }

            yield return null; // 안정용 1프레임
        }

        if (debugLog) Debug.Log($"[CutsceneRouter] END key='{key}'");

        EndInputLockIfHeld();
        _running = false;
        s_runningCutsceneCount = Mathf.Max(0, s_runningCutsceneCount - 1);
    }

    private Route FindRoute(string key)
    {
        if (routes == null) return null;

        for (int i = 0; i < routes.Count; i++)
        {
            var r = routes[i];
            if (r != null && r.key == key)
                return r;
        }
        return null;
    }

    private PlayerMove ResolvePlayerMove()
    {
        var pmm = FindObjectOfType<PlayerMainManager>(true);
        if (pmm != null)
        {
            var pm = pmm.GetComponent<PlayerMove>();
            if (pm != null) return pm;
        }

        var pm2 = FindObjectOfType<PlayerMove>(true);
        if (pm2 != null) return pm2;

        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.GetComponent<PlayerMove>() : null;
    }

    // ------------------------------------------------------
    // Input Lock
    // ------------------------------------------------------
    private void BeginInputLock()
    {
        if (lockActionViaGameManager && GameManager.Instance != null && !_heldActionLock)
        {
            GameManager.Instance.LockAction();
            _heldActionLock = true;
        }

        if (disablePlayerMoveComponent && _pmCached == null)
        {
            _pmCached = FindObjectOfType<PlayerMove>(true);
            if (_pmCached != null)
            {
                _pmWasEnabled = _pmCached.enabled;
                _pmCached.enabled = false;
            }
        }
    }

    private void EndInputLockIfHeld()
    {
        if (_pmCached != null)
        {
            _pmCached.enabled = _pmWasEnabled;
            _pmCached = null;
            _pmWasEnabled = false;
        }

        if (_heldActionLock && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _heldActionLock = false;
        }
    }


    private void OnDisable()
    {
        // 씬 전환/오브젝트 비활성화로 코루틴이 중단될 때 입력 잠금이 남지 않도록 안전 해제
        EndInputLockIfHeld();
        if (_running)
            s_runningCutsceneCount = Mathf.Max(0, s_runningCutsceneCount - 1);
        _running = false;
    }


    [ContextMenu("Debug/Dump Lock State")]
    public void DebugDumpLockState()
    {
        bool gmAction = GameManager.Instance != null && GameManager.Instance.isAction;
        var pm = (_pmCached != null) ? _pmCached : ResolvePlayerMove();
        bool pmEnabled = (pm != null) && pm.enabled;
        string autoStartKey = ResolveAutoStartKey();

        Debug.Log(
            "[CutsceneRouter] LOCK STATE " +
            $"running={_running}, heldActionLock={_heldActionLock}, pmCached={(_pmCached != null)}, pmEnabled={pmEnabled}, gm.isAction={gmAction}, startKey='{startKey}', resolvedStartKey='{autoStartKey}', oneShotStartKey={oneShotStartKey}"
        );
    }

    private string ResolveAutoStartKey()
    {
        if (!string.IsNullOrWhiteSpace(_resolvedStartKey))
            return _resolvedStartKey;

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (CutsceneStartContext.TryConsume(activeSceneName, out string overrideStartKey))
        {
            _resolvedStartKey = overrideStartKey;

            if (debugLog)
                Debug.Log($"[CutsceneRouter] Use pending scene start key '{_resolvedStartKey}' for scene '{activeSceneName}'");

            return _resolvedStartKey;
        }

        _resolvedStartKey = startKey;
        return _resolvedStartKey;
    }

    private static string BuildStartKeyFlag(string key)
        => string.IsNullOrWhiteSpace(key) ? string.Empty : $"cutscene.start.used:{key}";

    private bool IsStartKeyAlreadyConsumed(string key)
    {
        string flag = BuildStartKeyFlag(key);
        if (string.IsNullOrEmpty(flag)) return false;

        var data = ProgressSaveStore.Load();
        return data.HasFlag(flag);
    }

    private void ConsumeStartKey(string key)
    {
        string flag = BuildStartKeyFlag(key);
        if (string.IsNullOrEmpty(flag)) return;

        var data = ProgressSaveStore.Load();
        if (data.HasFlag(flag)) return;

        data.AddFlag(flag);
        ProgressSaveStore.Save(data);

        if (debugLog) Debug.Log($"[CutsceneRouter] consumed AutoStart key flag '{flag}'");
    }
}
