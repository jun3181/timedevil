// Assets/Script/Scene/VisitEffect/SceneVisitEffectBase.cs
using System.Collections;
using UnityEngine;

public abstract class SceneVisitEffectBase : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] protected bool useUnscaledTime = true;

    /// <summary>씬에 "들어왔을 때" 재생되는 연출</summary>
    public abstract IEnumerator PlayEnter();

    /// <summary>씬을 "나가기 직전" 재생되는 연출</summary>
    public abstract IEnumerator PlayExit();

    protected float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    protected IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f) yield break;
        if (useUnscaledTime) yield return new WaitForSecondsRealtime(seconds);
        else yield return new WaitForSeconds(seconds);
    }
}
