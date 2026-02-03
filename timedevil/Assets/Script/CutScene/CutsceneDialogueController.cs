// Assets/Script/Cutscene/CutsceneDialogueController.cs
using System;
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class CutsceneDialogueController : MonoBehaviour
{
    [Header("Route (CutSceneManager에서 string으로 찾을 때 사용)")]
    public string cutsceneId = "intro_01";

    [Header("Timeline")]
    public PlayableDirector director;

    [Header("Dialogue (Signal마다 1줄씩)")]
    public Dialogue dialogue;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.E;

    [Header("Options")]
    public bool oneShot = true;          // 1회성
    public bool keepEndState = true;     // A 방식: 끝난 포즈/상태 유지(Hold)
    public bool lockPlayerInput = true;  // GameManager.isAction 잠금
    public bool startOnAwake = false;    // (나중에 자동 컷씬용)

    [Header("Debug")]
    public bool debugLog = true;

    public bool IsRunning => _running;
    public event Action<CutsceneDialogueController> OnFinished;

    private bool _waitingAtMarker = false;
    private bool _running = false;
    private bool _played = false;

    private bool _prevGameAction = false;
    private bool _prevDialogueBlock = false;

    private void Awake()
    {
        if (!director) director = GetComponent<PlayableDirector>();

        if (director)
            director.extrapolationMode = keepEndState ? DirectorWrapMode.Hold : DirectorWrapMode.None;
    }

    private void OnEnable()
    {
        if (director) director.stopped += HandleStopped;
    }

    private void OnDisable()
    {
        if (director) director.stopped -= HandleStopped;
    }

    private void Start()
    {
        if (startOnAwake) StartCutscene();
    }

    /// <summary>외부(Manager/Interactable/Trigger)에서 호출</summary>
    public bool StartCutscene()
    {
        if (_running) return false;
        if (oneShot && _played) return false;

        if (!director || director.playableAsset == null || dialogue == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    $"[CutsceneDialogueController] missing refs. " +
                    $"director={(director ? director.name : "null")}, " +
                    $"asset={(director && director.playableAsset ? director.playableAsset.name : "null")}, " +
                    $"dialogue={(dialogue != null)}",
                    this
                );
            }
            return false;
        }

        _played = true;
        _running = true;
        _waitingAtMarker = false;

        director.extrapolationMode = keepEndState ? DirectorWrapMode.Hold : DirectorWrapMode.None;

        // 입력 잠금 백업/적용
        if (GameManager.Instance != null)
        {
            _prevGameAction = GameManager.Instance.isAction;
            if (lockPlayerInput) GameManager.Instance.isAction = true;
        }

        // 대사: 컷씬 중 월드 입력으로 넘어가지 않게 blockInput=true
        var dm = DialogueManager.instance;
        if (dm != null)
        {
            _prevDialogueBlock = dm.blockInput;
            dm.blockInput = true;

            // 큐 채우기(첫 줄 자동 출력은 blockInput 때문에 막혀서 대기 상태가 됨)
            dm.StartDialogue(dialogue);
        }

        director.time = 0;
        director.Evaluate(); // 첫 프레임 상태 반영
        director.Play();

        if (debugLog) Debug.Log($"[CutsceneDialogueController] Start id='{cutsceneId}'", this);
        return true;
    }

    /// <summary>
    /// Timeline Signal에서 호출:
    /// - Pause
    /// - 대사 1줄 출력
    /// </summary>
    public void OnSignal_ShowNextLine()
    {
        if (!_running || !director) return;

        director.Pause();
        _waitingAtMarker = true;

        DialogueManager.instance?.Cutscene_DisplayNextSentence();

        if (debugLog) Debug.Log($"[CutsceneDialogueController] Signal -> show next line (id='{cutsceneId}')", this);
    }

    private void Update()
    {
        if (!_running || !_waitingAtMarker) return;
        if (!Input.GetKeyDown(advanceKey)) return;

        var dm = DialogueManager.instance;

        // 1) 타이핑 중이면 즉시 완성
        if (dm != null && dm.IsTyping)
        {
            dm.ForceCompleteTyping();
            return;
        }

        // 2) 타이핑 끝이면 다음 마커까지 진행
        _waitingAtMarker = false;
        director.Play();
    }

    private void HandleStopped(PlayableDirector d)
    {
        if (!_running) return;

        _running = false;
        _waitingAtMarker = false;

        // 컷씬 종료 시 대화창 정리
        var dm = DialogueManager.instance;
        if (dm != null)
        {
            dm.EndDialogueExternal();
            dm.blockInput = _prevDialogueBlock;
        }

        if (GameManager.Instance != null && lockPlayerInput)
            GameManager.Instance.isAction = _prevGameAction;

        if (debugLog) Debug.Log($"[CutsceneDialogueController] End id='{cutsceneId}'", this);

        OnFinished?.Invoke(this);
    }
}
