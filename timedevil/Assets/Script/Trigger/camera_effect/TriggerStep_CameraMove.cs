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
    [Header("Restore (Smooth)")]
    [Min(0f)][SerializeField] private float restoreDuration = 0.6f;
    [SerializeField] private AnimationCurve restoreEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool lockPlayerInputWhileRestoring = true;

    [Tooltip("TriggerGet에서 병행 시작 시 true 권장. false면 Execute 호출만으로도 바로 반환.")]
    [SerializeField] private bool runAsync = true;
    [SerializeField] private bool debugLog = false;

    private Coroutine _running;
    private Coroutine _restoreCo;
    private bool _lockedByRestore = false;
    private bool _hasSnapshot = false;
    private CameraModeId _prevMode = CameraModeId.Fixed;
    private float _prevOrtho = 5f;
    private Vector3 _prevFixedPos = Vector3.zero;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        BeginFromTriggerGet(ctx);
        if (runAsync) yield break;
        if (_running != null) yield return _running;
    }

    public void BeginFromTriggerGet(TriggerContext ctx)
    {
        CaptureSnapshotIfNeeded();
        if (_running != null)
            StopCoroutine(_running);

        _running = StartCoroutine(CoRun(ctx));
    }

    public void RestorePreviousMode()
    {
        if (!_hasSnapshot || CameraManager.Instance == null) return;

        if (_restoreCo != null)
            StopCoroutine(_restoreCo);

        _restoreCo = StartCoroutine(CoRestorePreviousModeSmooth());
    }

    private IEnumerator CoRestorePreviousModeSmooth()
    {
        var cm = CameraManager.Instance;
        if (cm == null) yield break;

        if (lockPlayerInputWhileRestoring && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            _lockedByRestore = true;

            var pm = Object.FindObjectOfType<PlayerMove>(true);
            if (pm != null)
                pm.SetMoveInput(0, 0, false, false, false, false);
        }

        Vector3 from = Camera.main ? Camera.main.transform.position : _prevFixedPos;
        Vector3 to = ResolveRestoreDestination();
        float z = from.z;
        float dur = Mathf.Max(0f, restoreDuration);

        if (dur > 0f)
        {
            float t = 0f;
            while (t < dur)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float k = (restoreEase != null) ? restoreEase.Evaluate(u) : u;
                Vector3 p = Vector3.LerpUnclamped(from, to, k);
                cm.SetFixed(new Vector3(p.x, p.y, z), _prevOrtho);
                yield return null;
            }
        }

        cm.SetFixed(new Vector3(to.x, to.y, z), _prevOrtho);
        ApplyRestoreMode(cm);

        if (_lockedByRestore && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _lockedByRestore = false;
        }

        if (debugLog) Debug.Log($"[TriggerStep_CameraMove] RestorePreviousMode (smooth) -> {_prevMode}, dur={dur:0.###}");
        _restoreCo = null;
    }

    private Vector3 ResolveRestoreDestination()
    {
        switch (_prevMode)
        {
            case CameraModeId.FollowFree:
            case CameraModeId.FollowConfined:
            {
                Transform target = ResolveFollowTarget(null);
                if (target != null) return target.position;
                break;
            }
        }

        return _prevFixedPos;
    }

    private void ApplyRestoreMode(CameraManager cm)
    {
        switch (_prevMode)
        {
            case CameraModeId.FollowFree:
            case CameraModeId.FollowConfined:
            {
                Transform target = ResolveFollowTarget(null);
                if (target != null) cm.SetFollowFree(target, _prevOrtho);
                else cm.SetFixed(_prevFixedPos, _prevOrtho);
                break;
            }
            case CameraModeId.Cutscene:
                cm.SetCutscene(_prevFixedPos, _prevOrtho);
                break;
            case CameraModeId.Fixed:
            default:
                cm.SetFixed(_prevFixedPos, _prevOrtho);
                break;
        }
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

    private void CaptureSnapshotIfNeeded()
    {
        var cm = CameraManager.Instance;
        if (cm == null) return;

        if (cm.TryGetSnapshot(out CameraModeId mode, out float ortho, out Vector3 fixedPos, out string _))
        {
            _prevMode = mode;
            _prevOrtho = ortho;
            _prevFixedPos = fixedPos;
            _hasSnapshot = true;
        }
    }
}
