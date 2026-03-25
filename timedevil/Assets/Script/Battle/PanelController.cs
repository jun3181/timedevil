using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Panel 메뉴 상호작용(E 제출)으로 전투 프레젠테이션을 토글한다.
///
/// 기본 상태(초기):
/// - enemyTargets: 화면에 보임
/// - gameplayTargets: 화면 아래(오프스크린)에서 대기
///
/// 토글 상태:
/// - enemyTargets: 아래로 내려가 화면 밖으로 이동
/// - gameplayTargets: 위로 올라와 화면에 보임
/// </summary>
public class BattlePanelViewToggle : MonoBehaviour
{
    public enum EaseType
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic
    }

    [Header("Trigger")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private int triggerMenuIndex = 2;

    [Header("Target Groups")]
    [Tooltip("적(연출용)으로 취급할 대상들. 게임플레이 보기로 전환하면 아래로 내려감")]
    [SerializeField] private List<Transform> enemyTargets = new List<Transform>();

    [Tooltip("그리드/캐릭터/전투 오브젝트 등 게임플레이 요소들. 시작 시 아래에서 대기했다가 위로 올라옴")]
    [SerializeField] private List<Transform> gameplayTargets = new List<Transform>();

    [SerializeField] private bool useLocalPosition = true;

    [Header("Offsets")]
    [Tooltip("게임플레이 보기 ON일 때 enemyTargets에 적용할 오프셋(보통 아래 음수 Y)")]
    [SerializeField] private Vector3 enemyHiddenOffset = new Vector3(0f, -650f, 0f);

    [Tooltip("게임플레이 보기 OFF일 때 gameplayTargets에 적용할 오프셋(보통 아래 음수 Y)")]
    [SerializeField] private Vector3 gameplayHiddenOffset = new Vector3(0f, -650f, 0f);

    [Header("Animation - Enemy")]
    [SerializeField, Min(0.01f)] private float enemyDuration = 0.35f;
    [SerializeField] private EaseType enemyEase = EaseType.EaseInOutCubic;

    [Header("Animation - Gameplay")]
    [SerializeField, Min(0.01f)] private float gameplayDuration = 0.35f;
    [SerializeField] private EaseType gameplayEase = EaseType.EaseInOutCubic;

    [Header("State")]
    [Tooltip("체크 시 시작부터 게임플레이 요소가 보이는 상태로 시작")]
    [SerializeField] private bool startInGameplayView = false;

    private readonly List<Vector3> enemyShownBase = new List<Vector3>();
    private readonly List<Vector3> gameplayShownBase = new List<Vector3>();

    private bool isGameplayView;
    private bool isAnimating;
    private Coroutine running;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
    }

    void Awake()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);

        CacheShownBasePositions();

        isGameplayView = startInGameplayView;
        ApplyImmediate(isGameplayView);
    }

    void OnEnable()
    {
        if (menu) menu.onSubmit.AddListener(OnMenuSubmit);
    }

    void OnDisable()
    {
        if (menu) menu.onSubmit.RemoveListener(OnMenuSubmit);
    }

    private void OnMenuSubmit(int index)
    {
        if (index != triggerMenuIndex) return;
        ToggleView();
    }

    public void ToggleView()
    {
        if (isAnimating) return;

        isGameplayView = !isGameplayView;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Co_Animate(isGameplayView));
    }

    public void SetGameplayView(bool on, bool immediate = false)
    {
        if (isAnimating && running != null)
        {
            StopCoroutine(running);
            running = null;
            isAnimating = false;
        }

        isGameplayView = on;

        if (immediate) ApplyImmediate(on);
        else running = StartCoroutine(Co_Animate(on));
    }

    [ContextMenu("Re-cache Shown Base From Current")]
    public void CacheShownBasePositions()
    {
        enemyShownBase.Clear();
        gameplayShownBase.Clear();

        for (int i = 0; i < enemyTargets.Count; i++)
            enemyShownBase.Add(GetPos(enemyTargets[i]));

        for (int i = 0; i < gameplayTargets.Count; i++)
            gameplayShownBase.Add(GetPos(gameplayTargets[i]));
    }

    private IEnumerator Co_Animate(bool gameplayView)
    {
        isAnimating = true;

        var enemyFrom = SnapshotCurrent(enemyTargets);
        var gameplayFrom = SnapshotCurrent(gameplayTargets);

        var enemyTo = BuildEnemyTargetPositions(gameplayView);
        var gameplayTo = BuildGameplayTargetPositions(gameplayView);

        float maxDuration = Mathf.Max(enemyDuration, gameplayDuration);
        float t = 0f;

        while (t < maxDuration)
        {
            t += Time.deltaTime;

            float enemyT = enemyDuration <= 0f ? 1f : Mathf.Clamp01(t / enemyDuration);
            float gameplayT = gameplayDuration <= 0f ? 1f : Mathf.Clamp01(t / gameplayDuration);

            float enemyK = EvaluateEase(enemyEase, enemyT);
            float gameplayK = EvaluateEase(gameplayEase, gameplayT);

            ApplyLerp(enemyTargets, enemyFrom, enemyTo, enemyK);
            ApplyLerp(gameplayTargets, gameplayFrom, gameplayTo, gameplayK);

            yield return null;
        }

        ApplyAbsolute(enemyTargets, enemyTo);
        ApplyAbsolute(gameplayTargets, gameplayTo);

        running = null;
        isAnimating = false;
    }

    private void ApplyImmediate(bool gameplayView)
    {
        ApplyAbsolute(enemyTargets, BuildEnemyTargetPositions(gameplayView));
        ApplyAbsolute(gameplayTargets, BuildGameplayTargetPositions(gameplayView));
    }

    private List<Vector3> BuildEnemyTargetPositions(bool gameplayView)
    {
        var list = new List<Vector3>(enemyTargets.Count);
        for (int i = 0; i < enemyTargets.Count; i++)
        {
            Vector3 shown = i < enemyShownBase.Count ? enemyShownBase[i] : GetPos(enemyTargets[i]);
            list.Add(gameplayView ? shown + enemyHiddenOffset : shown);
        }
        return list;
    }

    private List<Vector3> BuildGameplayTargetPositions(bool gameplayView)
    {
        var list = new List<Vector3>(gameplayTargets.Count);
        for (int i = 0; i < gameplayTargets.Count; i++)
        {
            Vector3 shown = i < gameplayShownBase.Count ? gameplayShownBase[i] : GetPos(gameplayTargets[i]);
            list.Add(gameplayView ? shown : shown + gameplayHiddenOffset);
        }
        return list;
    }

    private List<Vector3> SnapshotCurrent(List<Transform> targets)
    {
        var list = new List<Vector3>(targets.Count);
        for (int i = 0; i < targets.Count; i++)
            list.Add(GetPos(targets[i]));
        return list;
    }

    private void ApplyLerp(List<Transform> targets, List<Vector3> from, List<Vector3> to, float t)
    {
        int n = Mathf.Min(targets.Count, Mathf.Min(from.Count, to.Count));
        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;
            SetPos(tr, Vector3.LerpUnclamped(from[i], to[i], t));
        }
    }

    private void ApplyAbsolute(List<Transform> targets, List<Vector3> to)
    {
        int n = Mathf.Min(targets.Count, to.Count);
        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;
            SetPos(tr, to[i]);
        }
    }

    private Vector3 GetPos(Transform t)
    {
        if (!t) return Vector3.zero;
        return useLocalPosition ? t.localPosition : t.position;
    }

    private void SetPos(Transform t, Vector3 value)
    {
        if (!t) return;
        if (useLocalPosition) t.localPosition = value;
        else t.position = value;
    }

    private static float EvaluateEase(EaseType type, float x)
    {
        switch (type)
        {
            case EaseType.EaseInQuad: return x * x;
            case EaseType.EaseOutQuad: return 1f - (1f - x) * (1f - x);
            case EaseType.EaseInOutQuad:
                return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) * 0.5f;

            case EaseType.EaseInCubic: return x * x * x;
            case EaseType.EaseOutCubic: return 1f - Mathf.Pow(1f - x, 3f);
            case EaseType.EaseInOutCubic:
                return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f;

            default: return x;
        }
    }
}
