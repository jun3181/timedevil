// Assets/Script/Scene/VisitEffect/SceneVisitEffectRunner.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneVisitEffectRunner : MonoBehaviour
{
    public static SceneVisitEffectRunner Current { get; private set; }

    [Header("Effect (on this GameObject recommended)")]
    [SerializeField] private SceneVisitEffectBase effect;

    [Header("Auto Enter")]
    [SerializeField] private bool playEnterOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public static event Action OnEnterComplete;

    private bool _enterPlayed = false;
    private bool _transitioning = false;

    private void Awake()
    {
        if (Current != null && Current != this)
            Debug.LogWarning("[SceneVisitEffectRunner] 씬에 Runner가 2개입니다. 하나만 남기세요.");

        Current = this;
        if (!effect) effect = GetComponent<SceneVisitEffectBase>();
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private IEnumerator Start()
    {
        if (!playEnterOnStart) yield break;
        yield return PlayEnter();
    }

    public IEnumerator PlayEnter()
    {
        if (_enterPlayed) yield break;
        _enterPlayed = true;

        if (debugLog) Debug.Log("[SceneVisitEffectRunner] Enter start");
        if (effect != null) yield return effect.PlayEnter();
        if (debugLog) Debug.Log("[SceneVisitEffectRunner] Enter complete");

        OnEnterComplete?.Invoke();
    }

    public IEnumerator PlayExit()
    {
        if (debugLog) Debug.Log("[SceneVisitEffectRunner] Exit start");
        if (effect != null) yield return effect.PlayExit();
        if (debugLog) Debug.Log("[SceneVisitEffectRunner] Exit complete");
    }

    // 기존 호출 유지 + LoadMode도 선택 가능
    public void LoadSceneWithExitEffect(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (_transitioning) return;
        StartCoroutine(CoLoadScene(sceneName, mode));
    }

    private IEnumerator CoLoadScene(string sceneName, LoadSceneMode mode)
    {
        _transitioning = true;

        yield return PlayExit();

        SceneManager.LoadScene(sceneName, mode);
    }
}
