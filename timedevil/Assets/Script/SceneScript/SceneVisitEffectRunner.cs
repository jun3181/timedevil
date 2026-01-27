// Assets/Script/Scene/VisitEffect/SceneVisitEffectRunner.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneVisitEffectRunner : MonoBehaviour
{
    // "씬마다 1개"만 존재하는 Current (싱글톤처럼 쓰되 DontDestroy 아님)
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
        {
            Debug.LogWarning("[SceneVisitEffectRunner] 씬에 Runner가 2개입니다. 하나만 남기세요.");
        }
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

        if (effect != null)
            yield return effect.PlayEnter();

        if (debugLog) Debug.Log("[SceneVisitEffectRunner] Enter complete");
        OnEnterComplete?.Invoke();
    }

    public IEnumerator PlayExit()
    {
        if (debugLog) Debug.Log("[SceneVisitEffectRunner] Exit start");

        if (effect != null)
            yield return effect.PlayExit();

        if (debugLog) Debug.Log("[SceneVisitEffectRunner] Exit complete");
    }

    /// <summary>
    /// 현재 씬에서 Exit 연출을 재생한 뒤 다음 씬 로드
    /// </summary>
    public void LoadSceneWithExitEffect(string sceneName)
    {
        if (_transitioning) return;
        StartCoroutine(CoLoadScene(sceneName));
    }

    private IEnumerator CoLoadScene(string sceneName)
    {
        _transitioning = true;

        yield return PlayExit();

        // 씬 로드(다음 씬은 그 씬의 Runner가 Enter를 담당)
        SceneManager.LoadScene(sceneName);
    }
}
