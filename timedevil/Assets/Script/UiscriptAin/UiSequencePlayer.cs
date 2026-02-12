// Assets/Script/UI/UiSequencePlayer.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiSequencePlayer : MonoBehaviour
{
    [Header("순서대로 보여줄 오브젝트")]
    [SerializeField] private List<GameObject> uiSteps = new List<GameObject>();

    [Header("입력 키")]
    [SerializeField] private KeyCode nextKey = KeyCode.E;

    [Header("씬 시작 시 자동 시작")]
    [SerializeField] private bool playOnStart = true;

    [Header("Myroom 1회 정책")]
    [Tooltip("이 씬에서만 자동 재생")]
    [SerializeField] private bool restrictToScene = true;
    [SerializeField] private string onlySceneName = "Myroom";

    [Tooltip("저장(세이브 파일)이 있으면 자동 재생 스킵")]
    [SerializeField] private bool skipIfSaveExists = true;

    [Tooltip("세이브가 있다고 판단하는 파일들(persistentDataPath 기준). 하나라도 있으면 '세이브 있음'")]
    [SerializeField] private List<string> saveFiles = new List<string> { "player.json", "cards.json" };

    [Tooltip("이미 봤으면(1) 자동 재생 스킵")]
    [SerializeField] private string seenPrefKey = "Myroom_UISequence_Seen_v1";

    [Header("완료 처리")]
    [SerializeField] private bool markSeenOnFinish = true;
    [SerializeField] private bool hideAllWhenFinished = true;
    [SerializeField] private bool loop = false;

    [Header("Input Lock (WASD 완전 차단)")]
    [Tooltip("UI 시퀀스 동안 GameManager.LockAction()/UnlockAction() 사용")]
    [SerializeField] private bool lockActionViaGameManager = true;

    [Tooltip("UI 시퀀스 동안 PlayerMove 컴포넌트를 꺼서 입력을 완전히 차단")]
    [SerializeField] private bool disablePlayerMoveComponent = true;

    public event Action OnFinished;

    private int index = 0;
    private bool isPlaying = false;

    // lock runtime
    private bool _heldActionLock = false;
    private PlayerMove _pmCached = null;
    private bool _pmWasEnabled = false;

    private void Start()
    {
        SetAllActive(false);

        if (playOnStart && ShouldAutoPlayNow())
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
        // 씬 전환/파괴 시 잠금이 남아있으면 복구
        EndInputLockIfHeld();
    }

    // ------------------------------------------------------

    private bool ShouldAutoPlayNow()
    {
        if (restrictToScene && SceneManager.GetActiveScene().name != onlySceneName)
            return false;

        if (!string.IsNullOrEmpty(seenPrefKey) && PlayerPrefs.GetInt(seenPrefKey, 0) == 1)
            return false;

        if (skipIfSaveExists && HasAnySaveFile())
            return false;

        return true;
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

    // ------------------------------------------------------

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

        // 현재 끄기
        SetStepActive(index, false);
        index++;

        // 끝 처리
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

        // 다음 켜기
        SetStepActive(index, true);
    }

    public void StopAndHideAll()
    {
        isPlaying = false;
        SetAllActive(false);
        EndInputLockIfHeld();
    }

    // 디버그용: "Myroom 튜토리얼 다시 보기"
    public void ResetSeenFlag()
    {
        if (!string.IsNullOrEmpty(seenPrefKey))
        {
            PlayerPrefs.DeleteKey(seenPrefKey);
            PlayerPrefs.Save();
        }
    }

    // ------------------------------------------------------
    // Input Lock
    // ------------------------------------------------------

    private void BeginInputLock()
    {
        // 1) GameManager Action Lock
        if (lockActionViaGameManager && GameManager.Instance != null && !_heldActionLock)
        {
            GameManager.Instance.LockAction();
            _heldActionLock = true;
        }

        // 2) PlayerMove 컴포넌트 disable (확실 차단)
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
        // PlayerMove 복구
        if (_pmCached != null)
        {
            _pmCached.enabled = _pmWasEnabled;
            _pmCached = null;
            _pmWasEnabled = false;
        }

        // GameManager Action Unlock
        if (_heldActionLock && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _heldActionLock = false;
        }
    }

    // ------------------------------------------------------

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
