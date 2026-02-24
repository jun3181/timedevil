// Assets/Script/UI/UiSequencePlayer.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiSequencePlayer : MonoBehaviour
{
    public enum AutoPlayPolicy
    {
        Always,
        OnlyNewGame,
        OnlyLoadGame
    }

    [Header("순서대로 보여줄 오브젝트")]
    [SerializeField] private List<GameObject> uiSteps = new List<GameObject>();

    [Header("입력 키")]
    [SerializeField] private KeyCode nextKey = KeyCode.E;

    [Header("씬 시작 시 자동 시작")]
    [SerializeField] private bool playOnStart = true;

    [Header("자동재생 조건")]
    [SerializeField] private AutoPlayPolicy autoPlayPolicy = AutoPlayPolicy.OnlyNewGame;

    [Header("Scene 제한(선택)")]
    [SerializeField] private bool restrictToScene = true;
    [SerializeField] private string onlySceneName = "Myroom";

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

    private int index = 0;
    private bool isPlaying = false;

    // lock runtime
    private bool _heldActionLock = false;
    private PlayerMove _pmCached = null;
    private bool _pmWasEnabled = false;

    // NewGame 토큰당 1번만 seen 키 초기화
    private static int s_lastResetToken = -1;

    private void Start()
    {
        SetAllActive(false);

        if (!playOnStart) return;
        if (!ShouldAutoPlayNow()) return;

        // ✅ 저장 파일이 하나라도 있으면 자동 스킵
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

        PlayFromStart();
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (uiSteps == null || uiSteps.Count == 0) return;

        if (Input.GetKeyDown(nextKey))
            Next();
    }

    private void OnDestroy()
    {
        EndInputLockIfHeld();
    }

    private bool ShouldAutoPlayNow()
    {
        if (restrictToScene && SceneManager.GetActiveScene().name != onlySceneName)
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
        if (uiSteps == null || uiSteps.Count == 0) return;

        SetAllActive(false);
        index = 0;
        isPlaying = true;

        BeginInputLock();
        SetStepActive(index, true);
    }

    public void Next()
    {
        if (uiSteps == null || uiSteps.Count == 0) return;

        SetStepActive(index, false);
        index++;

        if (index >= uiSteps.Count)
        {
            if (loop)
            {
                index = 0;
                SetStepActive(index, true);
                return;
            }

            isPlaying = false;

            if (hideAllWhenFinished)
                SetAllActive(false);

            if (markSeenOnFinish && !string.IsNullOrEmpty(seenPrefKey))
            {
                PlayerPrefs.SetInt(seenPrefKey, 1);
                PlayerPrefs.Save();
            }

            EndInputLockIfHeld();
            OnFinished?.Invoke();
            return;
        }

        SetStepActive(index, true);
    }

    public void StopAndHideAll()
    {
        isPlaying = false;
        SetAllActive(false);
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

    private void SetAllActive(bool active)
    {
        if (uiSteps == null) return;

        for (int i = 0; i < uiSteps.Count; i++)
        {
            if (uiSteps[i] != null)
                uiSteps[i].SetActive(active);
        }
    }

    private void SetStepActive(int stepIndex, bool active)
    {
        if (uiSteps == null) return;
        if (stepIndex < 0 || stepIndex >= uiSteps.Count) return;

        if (uiSteps[stepIndex] != null)
            uiSteps[stepIndex].SetActive(active);
    }
}