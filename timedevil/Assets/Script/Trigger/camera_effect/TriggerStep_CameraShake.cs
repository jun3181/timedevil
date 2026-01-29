// Assets/Script/Trigger/Steps/TriggerStep_CameraShake.cs
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(10000)] // CinemachineBrain(LateUpdate) 이후에 실행되도록 크게
[DisallowMultipleComponent]
public class TriggerStep_CameraShake : TriggerStepBase
{
    [Header("Target Camera (비우면 Camera.main)")]
    [SerializeField] private Camera targetCamera;

    [Header("Shake")]
    [Min(0.01f)][SerializeField] private float duration = 0.25f;
    [Min(0f)][SerializeField] private float amplitude = 0.25f; // 월드 단위
    [Min(0f)][SerializeField] private float frequency = 18f;

    [Header("Fade Out")]
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Flow")]
    [Tooltip("true면 흔들림이 끝날 때까지 다음 Step으로 안 넘어감")]
    [SerializeField] private bool waitUntilDone = true;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    // runtime
    private float _timeLeft;
    private float _total;
    private float _amp;
    private float _freq;
    private float _seedX;
    private float _seedY;
    private bool _shaking;

    private void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;
        _seedX = Random.Range(0f, 9999f);
        _seedY = Random.Range(0f, 9999f);
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!targetCamera)
        {
            Debug.LogWarning("[TriggerStep_CameraShake] targetCamera가 없습니다. (MainCamera 태그/Camera.main 확인)");
            yield break;
        }

        StartShake(duration, amplitude, frequency);

        if (!waitUntilDone) yield break;

        // shake 끝날 때까지 대기
        while (_shaking) yield return null;
    }

    public void StartShake(float dur, float amp, float freq)
    {
        _total = Mathf.Max(0.01f, dur);
        _timeLeft = _total;
        _amp = Mathf.Max(0f, amp);
        _freq = Mathf.Max(0f, freq);
        _shaking = true;

        if (debugLog)
            Debug.Log($"[TriggerStep_CameraShake] Start dur={_total} amp={_amp} freq={_freq}");
    }

    private void LateUpdate()
    {
        if (!_shaking || !targetCamera) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _timeLeft -= dt;

        // Cinemachine이 이미 최종 위치를 만들어 둔 상태(=지금 카메라 위치)가 base
        Vector3 basePos = targetCamera.transform.position;

        float elapsed = Mathf.Clamp01((_total - _timeLeft) / _total);
        float strength = 1f;

        if (fadeOut)
        {
            // 0..1 => 1..0(기본)
            strength = (fadeCurve != null) ? fadeCurve.Evaluate(elapsed) : (1f - elapsed);
        }

        float t = (useUnscaledTime ? Time.unscaledTime : Time.time) * _freq;

        // -1 ~ 1 범위 노이즈
        float nx = (Mathf.PerlinNoise(_seedX, t) * 2f - 1f);
        float ny = (Mathf.PerlinNoise(_seedY, t) * 2f - 1f);

        Vector3 offset = new Vector3(nx, ny, 0f) * (_amp * strength);

        targetCamera.transform.position = basePos + offset;

        if (_timeLeft <= 0f)
        {
            _shaking = false;
            if (debugLog) Debug.Log("[TriggerStep_CameraShake] End");
        }
    }
}
