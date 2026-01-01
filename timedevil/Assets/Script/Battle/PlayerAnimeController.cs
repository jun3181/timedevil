using System.Collections;
using UnityEngine;

public class PlayerAnimeController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;  

    [Header("Default Ease")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool IsPlaying { get; private set; }
    Coroutine playingCR;

    public void SetTarget(Transform t) => target = t;

    public void StopAll()
    {
        if (playingCR != null) StopCoroutine(playingCR);
        playingCR = null;
        IsPlaying = false;
    }

    /// <summary>start → end로 duration 동안 보간</summary>
    public Coroutine AnimateTo(Vector3 end, float duration, AnimationCurve customEase = null)
    {
        if (!target) return null;
        StopAll();
        playingCR = StartCoroutine(Co_Move(target.position, end, duration, customEase));
        return playingCR;
    }

    /// <summary>start → end → start 왕복(halfDuration*2) + holdAtEnd</summary>
    public Coroutine AnimatePingPong(Vector3 end, float halfDuration, float holdAtEnd, AnimationCurve customEase = null)
    {
        if (!target) return null;
        StopAll();
        playingCR = StartCoroutine(Co_PingPong(target.position, end, halfDuration, holdAtEnd, customEase));
        return playingCR;
    }

    IEnumerator Co_Move(Vector3 start, Vector3 end, float duration, AnimationCurve customEase)
    {
        IsPlaying = true;
        float t = 0f;
        var curve = (customEase != null) ? customEase : ease;   // 🔧 FIX
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = curve.Evaluate(u);
            target.position = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }
        target.position = end;
        IsPlaying = false;
        playingCR = null;
    }

    IEnumerator Co_PingPong(Vector3 start, Vector3 end, float half, float hold, AnimationCurve customEase)
    {
        IsPlaying = true;
        var curve = (customEase != null) ? customEase : ease;   // 🔧 FIX
        half = Mathf.Max(0.01f, half);

        // go
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / half);
            float k = curve.Evaluate(u);
            target.position = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }
        target.position = end;

        if (hold > 0f) yield return new WaitForSeconds(hold);

        // back
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / half);
            float k = curve.Evaluate(u);
            target.position = Vector3.LerpUnclamped(end, start, k);
            yield return null;
        }
        target.position = start;

        IsPlaying = false;
        playingCR = null;
    }
}
