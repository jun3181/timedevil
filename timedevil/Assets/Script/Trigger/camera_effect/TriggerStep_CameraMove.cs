using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_CameraMove : TriggerStepBase
{
    public enum CameraMoveMode
    {
        FollowPlayer,
        FixedPosition,
        MoveToPosition
    }

    [Header("Mode")]
    [SerializeField] private CameraMoveMode mode = CameraMoveMode.FollowPlayer;

    [Header("FollowPlayer")]
    [SerializeField] private Transform followTargetOverride;
    [SerializeField] private float? followOrthoSize = null;

    [Header("FixedPosition")]
    [SerializeField] private Transform fixedAnchor;
    [SerializeField] private Vector3 fixedWorldPosition;
    [SerializeField] private bool useFixedAnchorTransform = true;
    [SerializeField] private float fixedOrthoSize = 5f;

    [Header("MoveToPosition")]
    [SerializeField] private Transform moveTarget;
    [SerializeField] private Vector3 moveTargetWorldPosition;
    [Min(0f)][SerializeField] private float moveDuration = 1f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Flow")]
    [Tooltip("TriggerGet에서 병행 시작 시 true 권장. false면 Execute 호출만으로도 바로 반환.")]
    [SerializeField] private bool runAsync = true;
    [SerializeField] private bool debugLog = false;

    private Coroutine _running;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        BeginFromTriggerGet(ctx);
        if (runAsync) yield break;
        if (_running != null) yield return _running;
    }

    public void BeginFromTriggerGet(TriggerContext ctx)
    {
        if (_running != null)
            StopCoroutine(_running);

        _running = StartCoroutine(CoRun(ctx));
    }

    private IEnumerator CoRun(TriggerContext ctx)
    {
        var cm = CameraManager.Instance;
        if (cm == null) yield break;

        switch (mode)
        {
            case CameraMoveMode.FollowPlayer:
            {
                Transform target = ResolveFollowTarget(ctx);
                if (target != null)
                    cm.SetFollowFree(target, followOrthoSize);
                if (debugLog) Debug.Log($"[TriggerStep_CameraMove] FollowPlayer -> {(target ? target.name : "null")}");
                break;
            }
            case CameraMoveMode.FixedPosition:
            {
                Vector3 pos = ResolveFixedPosition();
                cm.SetFixed(pos, fixedOrthoSize);
                if (debugLog) Debug.Log($"[TriggerStep_CameraMove] FixedPosition -> {pos}");
                break;
            }
            case CameraMoveMode.MoveToPosition:
            {
                Vector3 to = ResolveMoveTarget();
                Vector3 from = Camera.main ? Camera.main.transform.position : to;
                float z = from.z;
                float dur = Mathf.Max(0f, moveDuration);

                if (dur <= 0f)
                {
                    cm.SetFixed(new Vector3(to.x, to.y, z), fixedOrthoSize);
                }
                else
                {
                    float t = 0f;
                    while (t < dur)
                    {
                        t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                        float u = Mathf.Clamp01(t / dur);
                        float k = (moveEase != null) ? moveEase.Evaluate(u) : u;
                        Vector3 p = Vector3.LerpUnclamped(from, to, k);
                        cm.SetFixed(new Vector3(p.x, p.y, z), fixedOrthoSize);
                        yield return null;
                    }
                }

                cm.SetFixed(new Vector3(to.x, to.y, z), fixedOrthoSize);
                if (debugLog) Debug.Log($"[TriggerStep_CameraMove] MoveToPosition -> {to}, dur={dur:0.###}");
                break;
            }
        }

        _running = null;
    }

    private Transform ResolveFollowTarget(TriggerContext ctx)
    {
        if (followTargetOverride) return followTargetOverride;
        if (ctx != null && ctx.player != null) return ctx.player;
        if (ctx != null && ctx.playerMove != null) return ctx.playerMove.transform;
        var pm = Object.FindObjectOfType<PlayerMove>(true);
        return pm ? pm.transform : null;
    }

    private Vector3 ResolveFixedPosition()
    {
        if (useFixedAnchorTransform && fixedAnchor != null) return fixedAnchor.position;
        return fixedWorldPosition;
    }

    private Vector3 ResolveMoveTarget()
    {
        if (moveTarget != null) return moveTarget.position;
        return moveTargetWorldPosition;
    }
}
