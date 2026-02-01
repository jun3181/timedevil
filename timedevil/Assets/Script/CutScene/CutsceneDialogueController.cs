using UnityEngine;
using UnityEngine.Playables;

public class CutsceneDialogueController : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector director;

    [Header("Dialogue")]
    public Dialogue dialogue;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.E;

    [Header("Options")]
    public bool lockPlayerInput = true; // GameManager.isAction 같은거 쓰면 true
    public bool startOnAwake = false;

    private bool _waitingAtMarker = false;

    private void Awake()
    {
        if (!director) director = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        if (director) director.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (director) director.stopped -= OnTimelineStopped;
    }

    private void Start()
    {
        if (startOnAwake) StartCutscene();
    }

    public void StartCutscene()
    {
        if (!director || dialogue == null) return;

        if (lockPlayerInput && GameManager.Instance)
            GameManager.Instance.isAction = true;

        // 컷씬 중엔 월드 입력으로 대사 넘어가지 않게 차단
        if (DialogueManager.instance)
            DialogueManager.instance.blockInput = true;

        // 큐만 채우고(첫줄 자동 출력은 blockInput 때문에 막힘)
        DialogueManager.instance?.StartDialogue(dialogue);

        director.time = 0;
        director.Play();
    }

    /// <summary>
    /// Timeline Signal에서 호출할 함수:
    /// - Timeline Pause
    /// - 대사 1줄 출력
    /// </summary>
    public void OnSignal_ShowNextLine()
    {
        if (!director) return;

        director.Pause();
        _waitingAtMarker = true;

        DialogueManager.instance?.Cutscene_DisplayNextSentence();
    }

    private void Update()
    {
        if (!_waitingAtMarker) return;
        if (!Input.GetKeyDown(advanceKey)) return;

        var dm = DialogueManager.instance;
        if (dm == null) return;

        // 1) 타이핑 중이면: 즉시 완성
        if (dm.IsTyping)
        {
            dm.ForceCompleteTyping();
            return;
        }

        // 2) 타이핑 끝이면: 다음 마커까지 진행
        _waitingAtMarker = false;
        director.Play();
    }

    private void OnTimelineStopped(PlayableDirector d)
    {
        _waitingAtMarker = false;

        // 컷씬 종료 시 대화창 정리
        var dm = DialogueManager.instance;
        if (dm)
        {
            dm.EndDialogueExternal();
            dm.blockInput = false;
        }

        if (lockPlayerInput && GameManager.Instance)
            GameManager.Instance.isAction = false;
    }
}
