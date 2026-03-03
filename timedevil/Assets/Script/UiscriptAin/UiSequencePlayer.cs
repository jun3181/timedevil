// Assets/Script/UI/UiSequencePlayer.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-30000)]
public class UiSequencePlayer : MonoBehaviour
{
    public enum AutoPlayPolicy
    {
        Always,
        OnlyNewGame,
        OnlyLoadGame
    }

    [Serializable]
    private class SequenceStep
    {
        public enum StepType
        {
            Image,
            Dialogue
        }

        [Tooltip("이 Step의 타입")]
        public StepType type = StepType.Image;

        [Tooltip("type=Image일 때 표시할 오브젝트")]
        public GameObject uiObject;

        [Tooltip("type=Dialogue일 때 실행할 Dialogue")]
        public Dialogue dialogue;

        [Tooltip("이 Step을 넘길 때 사용할 키. None이면 기본 nextKey 사용")]
        public KeyCode advanceKey = KeyCode.None;
    }

    [Header("순서대로 보여줄 Step (Image/Dialogue)")]
    [SerializeField] private List<SequenceStep> sequenceSteps = new List<SequenceStep>();

    [Header("입력 키")]
    [SerializeField] private KeyCode nextKey = KeyCode.E;

    [Header("씬 시작 시 자동 시작")]
    [SerializeField] private bool playOnStart = true;

    [Header("자동재생 조건")]
    [SerializeField] private AutoPlayPolicy autoPlayPolicy = AutoPlayPolicy.OnlyNewGame;

    [Header("Scene 제한(선택)")]
    [SerializeField] private bool restrictToScene = true;
    [SerializeField] private string onlySceneName = "Myroom";
    [Tooltip("추가 허용 씬 이름들 (예: Move_Tutorial)")]
    [SerializeField] private List<string> additionalAllowedScenes = new List<string> { "Move_Tutorial" };
    [Tooltip("Move_Tutorial 씬에서는 저장/봤음(1회) 체크를 무시하고 자동 재생을 강제")]
    [SerializeField] private bool alwaysPlayInMoveTutorial = true;

    [Header("Save 파일 존재 시 자동 스킵(옵션)")]
    [SerializeField] private bool skipIfSaveExists = true;

    [SerializeField]
    private List<string> saveFiles = new List<string>
    {
        "player.json",
        "cards.json",
        "progress.json",
        "items_save.json"
    };

    [Header("봤음(1회) 정책")]
    [Tooltip("이미 봤으면(1) 자동 재생 스킵. (LoadGame 쪽에서 주로 유효)")]
    [SerializeField] private string seenPrefKey = "Myroom_UISequence_Seen_v1";
    [SerializeField] private bool markSeenOnFinish = true;

    [Tooltip("새 게임 버튼을 눌러서 들어온 경우, 이번 1회에 한해 seenPrefKey를 자동 초기화(=무조건 재생 보장)")]
    [SerializeField] private bool resetSeenOnNewGameStart = true;

    [Header("완료 처리")]
    [SerializeField] private bool hideAllWhenFinished = true;
    [SerializeField] private bool loop = false;

    [Header("Input Lock (WASD 완전 차단)")]
    [SerializeField] private bool lockActionViaGameManager = true;
    [SerializeField] private bool disablePlayerMoveComponent = true;

    public event Action OnFinished;

    public bool IsPlayingSequence => isPlaying;

    private int index = 0;
    private bool isPlaying = false;
    private bool _stepEntered = false;
    private bool _waitingKeyReleaseAfterDialogue = false;

    // lock runtime
    private bool _heldActionLock = false;
    private PlayerMove _pmCached = null;
    private bool _pmWasEnabled = false;

    // NewGame 토큰당 1번만 seen 키 초기화
    private static int s_lastResetToken = -1;

    private void Start()
    {
        SetAllImagesActive(false);

        if (!playOnStart) return;
        if (!ShouldAutoPlayNow()) return;

        bool forcePlayInMoveTutorial = alwaysPlayInMoveTutorial && SceneManager.GetActiveScene().name == "Move_Tutorial";

        if (!forcePlayInMoveTutorial)
        {
            // ✅ 먼저 시작하더라도 저장 파일이 하나라도 있으면 자동 스킵
            if (skipIfSaveExists && HasAnySaveFile())
                return;

            // ✅ 새 게임에서만: 이번 "버튼 클릭 토큰"에 대해 1회 seen 초기화
            if (resetSeenOnNewGameStart &&
                GameStartContext.Mode == GameStartMode.NewGame &&
                s_lastResetToken != GameStartContext.StartToken)
            {
                s_lastResetToken = GameStartContext.StartToken;

                if (!string.IsNullOrEmpty(seenPrefKey))
                {
                    PlayerPrefs.DeleteKey(seenPrefKey);
                    PlayerPrefs.Save();
                }
            }

            // ✅ (seen 체크는 초기화 이후에)
            if (!string.IsNullOrEmpty(seenPrefKey) && PlayerPrefs.GetInt(seenPrefKey, 0) == 1)
                return;
        }

        PlayFromStart();
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (sequenceSteps == null || sequenceSteps.Count == 0) return;

        EnterCurrentStepIfNeeded();

        KeyCode currentAdvanceKey = GetCurrentStepAdvanceKey();

        // Dialogue Step 진행 중이면 진행 키 입력은 DialogueManager 쪽에서 소비
        if (IsCurrentStepDialogue())
        {
            var dm = DialogueManager.instance;

            if (dm != null && dm.isDialogueActive)
                return;

            // 대화 종료 직후 같은 키 입력으로 다음 Step이 스킵되지 않도록 키를 한번 떼게 함
            if (_waitingKeyReleaseAfterDialogue)
            {
                if (Input.GetKey(currentAdvanceKey)) return;
                _waitingKeyReleaseAfterDialogue = false;
            }
        }

        if (Input.GetKeyDown(currentAdvanceKey))
            Next();
    }

    private void OnDestroy()
    {
        EndInputLockIfHeld();
    }

    private bool ShouldAutoPlayNow()
    {
        if (restrictToScene && !IsAllowedScene(SceneManager.GetActiveScene().name))
            return false;

        switch (autoPlayPolicy)
        {
            case AutoPlayPolicy.Always:
                return true;
            case AutoPlayPolicy.OnlyNewGame:
                return GameStartContext.Mode == GameStartMode.NewGame;
            case AutoPlayPolicy.OnlyLoadGame:
                return GameStartContext.Mode == GameStartMode.LoadGame;
            default:
                return true;
        }
    }


    private bool IsAllowedScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (sceneName == onlySceneName)
            return true;

        if (additionalAllowedScenes == null)
            return false;

        for (int i = 0; i < additionalAllowedScenes.Count; i++)
        {
            var allowed = additionalAllowedScenes[i];
            if (!string.IsNullOrWhiteSpace(allowed) && sceneName == allowed)
                return true;
        }

        return false;
    }

    private KeyCode GetCurrentStepAdvanceKey()
    {
        if (sequenceSteps != null && index >= 0 && index < sequenceSteps.Count)
        {
            var step = sequenceSteps[index];
            if (step != null && step.advanceKey != KeyCode.None)
                return step.advanceKey;
        }

        return nextKey;
    }

    private bool HasAnySaveFile()
    {
        if (saveFiles == null || saveFiles.Count == 0) return false;

        string root = Application.persistentDataPath;
        for (int i = 0; i < saveFiles.Count; i++)
        {
            string f = saveFiles[i];
            if (string.IsNullOrWhiteSpace(f)) continue;

            string full = Path.Combine(root, f);
            if (File.Exists(full)) return true;
        }
        return false;
    }

    public void PlayFromStart()
    {
        if (sequenceSteps == null || sequenceSteps.Count == 0) return;

        SetAllImagesActive(false);
        index = 0;
        isPlaying = true;
        _stepEntered = false;
        _waitingKeyReleaseAfterDialogue = false;

        BeginInputLock();
        EnterCurrentStepIfNeeded();
    }

    public void Next()
    {
        if (sequenceSteps == null || sequenceSteps.Count == 0) return;

        if (IsCurrentStepDialogue())
        {
            var dm = DialogueManager.instance;
            if (dm != null && dm.isDialogueActive)
                return;
        }
        else
        {
            SetImageStepActive(index, false);
        }

        index++;

        if (index >= sequenceSteps.Count)
        {
            if (loop)
            {
                index = 0;
                SetAllImagesActive(false);
                _stepEntered = false;
                _waitingKeyReleaseAfterDialogue = false;
                EnterCurrentStepIfNeeded();
                return;
            }

            isPlaying = false;

            if (hideAllWhenFinished)
                SetAllImagesActive(false);

            if (markSeenOnFinish && !string.IsNullOrEmpty(seenPrefKey))
            {
                PlayerPrefs.SetInt(seenPrefKey, 1);
                PlayerPrefs.Save();
            }

            EndInputLockIfHeld();
            OnFinished?.Invoke();
            return;
        }

        _stepEntered = false;
        _waitingKeyReleaseAfterDialogue = false;
        EnterCurrentStepIfNeeded();
    }

    public void StopAndHideAll()
    {
        isPlaying = false;
        SetAllImagesActive(false);
        _stepEntered = false;
        _waitingKeyReleaseAfterDialogue = false;
        EndInputLockIfHeld();
    }

    public void ResetSeenFlag()
    {
        if (!string.IsNullOrEmpty(seenPrefKey))
        {
            PlayerPrefs.DeleteKey(seenPrefKey);
            PlayerPrefs.Save();
        }
    }

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

    private void SetAllImagesActive(bool active)
    {
        if (sequenceSteps == null) return;

        for (int i = 0; i < sequenceSteps.Count; i++)
        {
            if (sequenceSteps[i] != null && sequenceSteps[i].type == SequenceStep.StepType.Image && sequenceSteps[i].uiObject != null)
                sequenceSteps[i].uiObject.SetActive(active);
        }
    }

    private void SetImageStepActive(int stepIndex, bool active)
    {
        if (sequenceSteps == null) return;
        if (stepIndex < 0 || stepIndex >= sequenceSteps.Count) return;
        if (sequenceSteps[stepIndex] == null) return;
        if (sequenceSteps[stepIndex].type != SequenceStep.StepType.Image) return;

        if (sequenceSteps[stepIndex].uiObject != null)
            sequenceSteps[stepIndex].uiObject.SetActive(active);
    }

    private bool IsCurrentStepDialogue()
    {
        if (sequenceSteps == null) return false;
        if (index < 0 || index >= sequenceSteps.Count) return false;

        var step = sequenceSteps[index];
        return step != null && step.type == SequenceStep.StepType.Dialogue;
    }

    private void EnterCurrentStepIfNeeded()
    {
        if (_stepEntered) return;
        if (sequenceSteps == null) return;
        if (index < 0 || index >= sequenceSteps.Count) return;

        var step = sequenceSteps[index];
        if (step == null)
        {
            _stepEntered = true;
            return;
        }

        if (step.type == SequenceStep.StepType.Image)
        {
            if (step.uiObject != null)
                step.uiObject.SetActive(true);

            _stepEntered = true;
            return;
        }

        var dm = DialogueManager.instance;
        if (dm == null)
        {
            Debug.LogWarning("[UiSequencePlayer] DialogueManager.instance not found.");
            return;
        }

        if (step.dialogue == null)
        {
            _stepEntered = true;
            return;
        }

        if (dm.isDialogueActive) return;

        dm.StartDialogue(step.dialogue);
        _stepEntered = true;
        _waitingKeyReleaseAfterDialogue = true;
    }
}
