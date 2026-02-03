// Assets/Script/Cutscene/CutSceneManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CutSceneManager : MonoBehaviour
{
    private static CutSceneManager _instance;
    public static CutSceneManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<CutSceneManager>(true);
            return _instance;
        }
    }

    [Header("Options")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool rescanOnSceneLoaded = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly Dictionary<string, CutsceneDialogueController> _map = new();
    private CutsceneDialogueController _active;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;
        RescanSceneCutscenes();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _instance = null;
        }
    }

    private void HandleSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (rescanOnSceneLoaded)
            RescanSceneCutscenes();

        if (debugLog) Debug.Log($"[CutSceneManager] sceneLoaded '{s.name}', cutscenes={_map.Count}");
    }

    public void RescanSceneCutscenes()
    {
        _map.Clear();

        var all = FindObjectsOfType<CutsceneDialogueController>(true);
        foreach (var c in all)
        {
            if (c == null) continue;
            if (string.IsNullOrWhiteSpace(c.cutsceneId)) continue;

            if (_map.ContainsKey(c.cutsceneId))
            {
                Debug.LogWarning($"[CutSceneManager] duplicate cutsceneId='{c.cutsceneId}' (keep first, ignore '{c.name}')");
                continue;
            }

            _map.Add(c.cutsceneId, c);
        }
    }

    /// <summary>
    /// interaction/trigger에서 string id로 호출
    /// </summary>
    public bool Play(string cutsceneId)
    {
        if (string.IsNullOrWhiteSpace(cutsceneId))
        {
            Debug.LogWarning("[CutSceneManager] Play called with empty id");
            return false;
        }

        // 이미 실행 중이면 막기
        if (_active != null && _active.IsRunning)
            return false;

        if (!_map.TryGetValue(cutsceneId, out var controller) || controller == null)
        {
            // 스캔 타이밍 문제면 재스캔 후 재시도
            RescanSceneCutscenes();
            _map.TryGetValue(cutsceneId, out controller);
        }

        if (controller == null)
        {
            Debug.LogWarning($"[CutSceneManager] cutscene not found id='{cutsceneId}'");
            return false;
        }

        _active = controller;

        // 종료 이벤트 구독(중복 방지)
        _active.OnFinished -= HandleFinished;
        _active.OnFinished += HandleFinished;

        if (debugLog) Debug.Log($"[CutSceneManager] Play id='{cutsceneId}' -> '{controller.name}'");

        bool started = controller.StartCutscene();
        if (!started)
        {
            // oneShot이거나 실행 불가면 active 해제
            _active.OnFinished -= HandleFinished;
            _active = null;
        }

        return started;
    }

    private void HandleFinished(CutsceneDialogueController c)
    {
        if (_active == c)
        {
            _active.OnFinished -= HandleFinished;
            _active = null;
        }
    }
}
