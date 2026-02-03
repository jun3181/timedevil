// Assets/Script/CutScene/CutSceneManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CutSceneManager : MonoBehaviour
{
    public static CutSceneManager Instance { get; private set; }

    [Header("Options")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool rescanOnSceneLoaded = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly Dictionary<string, CutSceneEntry> _map = new Dictionary<string, CutSceneEntry>();

    private CutSceneEntry _active;
    private bool _waitingTimeline;
    private bool _waitingDialogue;

    private bool _prevGameAction;

    private Coroutine _watchCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;

        RescanSceneCutscenes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void HandleSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (rescanOnSceneLoaded) RescanSceneCutscenes();
        if (debugLog) Debug.Log($"[CutSceneManager] sceneLoaded '{s.name}' cutscenes={_map.Count}");
    }

    public void RescanSceneCutscenes()
    {
        _map.Clear();

        var all = FindObjectsOfType<CutSceneEntry>(true);
        foreach (var e in all)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.cutsceneId)) continue;

            if (_map.ContainsKey(e.cutsceneId))
            {
                Debug.LogWarning($"[CutSceneManager] duplicate id='{e.cutsceneId}' ignore '{e.name}'");
                continue;
            }

            _map.Add(e.cutsceneId, e);
        }
    }

    public bool IsPlaying => _active != null;

    /// <summary>
    /// Trigger/Interaction에서 key로 호출
    /// </summary>
    public bool Play(string cutsceneId)
    {
        if (string.IsNullOrWhiteSpace(cutsceneId))
        {
            Debug.LogWarning("[CutSceneManager] Play called with empty id");
            return false;
        }

        // 이미 컷씬 중이면 차단
        if (_active != null) return false;

        // 대화 중이면 차단(겹치면 꼬일 확률 큼)
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return false;

        if (!_map.TryGetValue(cutsceneId, out var entry) || entry == null)
        {
            // 타이밍 문제면 재스캔 후 1회 더
            RescanSceneCutscenes();
            _map.TryGetValue(cutsceneId, out entry);
        }

        if (entry == null)
        {
            Debug.LogWarning($"[CutSceneManager] not found id='{cutsceneId}'");
            return false;
        }

        // oneShot 체크
        if (entry.oneShot && entry.played)
            return false;

        if (!entry.IsValid(out var reason))
        {
            Debug.LogWarning($"[CutSceneManager] invalid entry id='{cutsceneId}' reason={reason}", entry);
            return false;
        }

        entry.played = true;
        _active = entry;

        // 잠금(이동/상호작용 막기) : Dialogue E는 PlayerMainManager 상단에서 처리하므로 살아있음
        if (entry.lockPlayerInput && GameManager.Instance != null)
        {
            _prevGameAction = GameManager.Instance.isAction;
            GameManager.Instance.isAction = true;
        }

        _waitingTimeline = entry.playTimeline;
        _waitingDialogue = entry.playDialogue;

        // 작동2: Dialogue (방식B = 일반 대화처럼 E로 넘김)
        if (entry.playDialogue && entry.dialogue != null && DialogueManager.instance != null)
        {
            // blockInput 절대 true로 하지 않는다 (E 넘김 정상 동작해야 함)
            DialogueManager.instance.blockInput = false;
            DialogueManager.instance.StartDialogue(entry.dialogue);
        }
        else
        {
            _waitingDialogue = false;
        }

        // 작동1: Timeline
        if (entry.playTimeline && entry.director != null)
        {
            entry.ApplyDirectorOptions();

            // stopped 이벤트 연결
            entry.director.stopped -= HandleDirectorStopped;
            entry.director.stopped += HandleDirectorStopped;

            entry.director.time = 0;
            entry.director.Evaluate();
            entry.director.Play();
        }
        else
        {
            _waitingTimeline = false;
        }

        if (_watchCo != null) StopCoroutine(_watchCo);
        _watchCo = StartCoroutine(CoWatchCompletion());

        if (debugLog) Debug.Log($"[CutSceneManager] Play '{cutsceneId}' -> '{entry.name}'", entry);
        return true;
    }

    private void HandleDirectorStopped(PlayableDirector d)
    {
        if (_active == null) return;
        if (_active.director != d) return;

        _waitingTimeline = false;
    }

    private IEnumerator CoWatchCompletion()
    {
        // Dialogue 끝 감시(방식B라서 E 입력으로 플레이어가 끝내야 함)
        while (_active != null)
        {
            if (_waitingDialogue)
            {
                if (DialogueManager.instance == null || DialogueManager.instance.isDialogueActive == false)
                    _waitingDialogue = false;
            }

            if (!_waitingTimeline && !_waitingDialogue)
                break;

            yield return null;
        }

        EndActiveCutscene();
    }

    private void EndActiveCutscene()
    {
        if (_active == null) return;

        // director 이벤트 해제
        if (_active.director != null)
            _active.director.stopped -= HandleDirectorStopped;

        // 잠금 복구
        if (_active.lockPlayerInput && GameManager.Instance != null)
            GameManager.Instance.isAction = _prevGameAction;

        if (debugLog) Debug.Log($"[CutSceneManager] End '{_active.cutsceneId}'", _active);

        _active = null;
        _waitingTimeline = false;
        _waitingDialogue = false;

        if (_watchCo != null)
        {
            StopCoroutine(_watchCo);
            _watchCo = null;
        }
    }
}
