// Assets/Script/Trigger/TriggerGet.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class TriggerGet : MonoBehaviour
{
    private struct RouteStageProgress
    {
        public readonly int stageIndex;
        public readonly int callCount;

        public RouteStageProgress(int stageIndex, int callCount)
        {
            this.stageIndex = stageIndex;
            this.callCount = callCount;
        }
    }

    // Keeps consumed trigger state across scene reloads during runtime.
    private static readonly Dictionary<string, int> s_callCountById = new();
    private static readonly Dictionary<string, RouteStageProgress> s_stageProgressById = new();

    [Header("Router")]
    public TriggerRouter router;

    [Header("Route Stages (Optional)")]
    [Tooltip("If entries exist, stages run in order and the fallback Route Key/Call Limit fields are ignored.")]
    public List<TriggerRouteStage> routeStages = new();

    [Header("Fallback Route Key")]
    public string routeKey = "Trigger1";

    [Header("Fallback Call Limit")]
    [Tooltip("0 means unlimited. 1 means one call. 2 means two calls.")]
    [Min(0)]
    public int maxCalls = 1;

    [Header("Detect")]
    [Tooltip("Checks the player by PlayerMove component.")]
    public bool usePlayerMoveComponentCheck = true;

    [Header("Grace Policy (Return from battle)")]
    [Tooltip("If true, this trigger is blocked during PlayerReturnContext grace period.")]
    public bool blockDuringGracePeriod = false;

    [Header("Debug")]
    public bool debugLog = true;

    [Header("Optional Parallel Step")]
    [Tooltip("Starts this camera step in parallel with the requested route.")]
    public TriggerStep_CameraMove cameraMoveStep;

    private int _called = 0;
    private int _stageIndex = 0;
    private int _stageCalled = 0;
    private Collider2D _selfCollider;

    private bool _pendingByCutscene = false;
    private Collider2D _pendingInstigatorCollider = null;
    private PlayerMove _pendingPlayerMove = null;
    private Coroutine _cameraRestoreCo = null;
    private string _runtimeId;

    private bool HasRouteStages => routeStages != null && routeStages.Count > 0;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        _runtimeId = BuildRuntimeId();

        _selfCollider = GetComponent<Collider2D>();
        if (!_selfCollider.isTrigger)
        {
            _selfCollider.isTrigger = true;
            if (debugLog) Debug.LogWarning("[TriggerGet] Collider2D.isTrigger was off, so it was enabled.");
        }

        if (!router) router = FindObjectOfType<TriggerRouter>(true);
        if (!cameraMoveStep) cameraMoveStep = GetComponent<TriggerStep_CameraMove>();

        RestoreProgress();
        ApplyConsumedStateIfNeeded();
    }

    private void Update()
    {
        if (!_pendingByCutscene) return;
        if (CutsceneRouter.IsAnyCutsceneRunning) return;

        if (_selfCollider != null && _pendingInstigatorCollider != null && _selfCollider.IsTouching(_pendingInstigatorCollider))
        {
            if (debugLog)
                Debug.Log($"[TriggerGet] Resume pending key='{GetRouteKeyForLog()}' after cutscene end by='{_pendingInstigatorCollider.name}'");

            TryInvokeRoute(_pendingInstigatorCollider, _pendingPlayerMove);
        }

        ClearPending();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!router)
        {
            if (debugLog) Debug.LogWarning("[TriggerGet] router is not assigned.");
            return;
        }

        if (!TryGetActiveRoute(out string currentRouteKey, out _, out _, out _))
        {
            ApplyConsumedStateIfNeeded();
            return;
        }

        if (blockDuringGracePeriod &&
            (PlayerReturnContext.IsInGracePeriod || PlayerReturnContext.GraceSecondsPending > 0f))
        {
            if (debugLog)
                Debug.Log($"[TriggerGet] Suppressed by Grace key='{currentRouteKey}' (by='{other.name}')");
            return;
        }

        PlayerMove pm = null;
        if (usePlayerMoveComponentCheck)
        {
            pm = other.GetComponent<PlayerMove>();
            if (!pm) return;
        }

        if (CutsceneRouter.IsAnyCutsceneRunning)
        {
            _pendingByCutscene = true;
            _pendingInstigatorCollider = other;
            _pendingPlayerMove = pm;

            if (debugLog)
                Debug.Log($"[TriggerGet] Deferred by cutscene key='{currentRouteKey}' by='{other.name}'");
            return;
        }

        TryInvokeRoute(other, pm);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_pendingByCutscene && other == _pendingInstigatorCollider)
            ClearPending();
    }

    private void TryInvokeRoute(Collider2D other, PlayerMove pm)
    {
        if (other == null) return;

        if (!TryGetActiveRoute(out string currentRouteKey, out int currentCallCount, out int currentMaxCalls, out int currentStageIndex))
        {
            ApplyConsumedStateIfNeeded();
            return;
        }

        if (string.IsNullOrWhiteSpace(currentRouteKey))
        {
            if (debugLog) Debug.LogWarning("[TriggerGet] routeKey is empty.", this);
            return;
        }

        RegisterRouteCall();
        currentCallCount++;

        if (debugLog)
        {
            string stageSuffix = currentStageIndex >= 0 ? $" stage={currentStageIndex + 1}/{routeStages.Count}" : "";
            Debug.Log($"[TriggerGet] Fired key='{currentRouteKey}' call={FormatCallCount(currentCallCount, currentMaxCalls)}{stageSuffix} by='{other.name}'");
        }

        var ctx = new TriggerContext(
            trigger: this,
            router: router,
            instigator: other.gameObject,
            instigatorCollider: other,
            playerMove: pm
        );

        if (cameraMoveStep != null)
        {
            cameraMoveStep.BeginFromTriggerGet(ctx);
            if (_cameraRestoreCo != null) StopCoroutine(_cameraRestoreCo);
            _cameraRestoreCo = StartCoroutine(CoRestoreCameraAfterRoute(currentRouteKey));
        }

        router.RequestRoute(currentRouteKey, ctx);

        ApplyConsumedStateIfNeeded();
    }

    private System.Collections.IEnumerator CoRestoreCameraAfterRoute(string key)
    {
        yield return null;
        while (router != null && router.IsRouteRunning(key))
            yield return null;

        if (cameraMoveStep != null)
            cameraMoveStep.RestorePreviousMode();

        _cameraRestoreCo = null;
    }

    private void ClearPending()
    {
        _pendingByCutscene = false;
        _pendingInstigatorCollider = null;
        _pendingPlayerMove = null;
    }

    private void RestoreProgress()
    {
        _called = 0;
        _stageIndex = 0;
        _stageCalled = 0;

        if (string.IsNullOrEmpty(_runtimeId)) return;

        if (HasRouteStages)
        {
            if (s_stageProgressById.TryGetValue(_runtimeId, out var persisted))
            {
                _stageIndex = Mathf.Clamp(persisted.stageIndex, 0, routeStages.Count);
                _stageCalled = Mathf.Max(0, persisted.callCount);
            }

            return;
        }

        if (s_callCountById.TryGetValue(_runtimeId, out int legacyPersisted))
            _called = Mathf.Max(0, legacyPersisted);
    }

    private void RegisterRouteCall()
    {
        if (HasRouteStages)
        {
            _stageCalled++;
            PersistRouteStageProgress();
            return;
        }

        _called++;
        PersistCallCount();
    }

    private void PersistCallCount()
    {
        if (string.IsNullOrEmpty(_runtimeId)) return;
        s_callCountById[_runtimeId] = _called;
    }

    private void PersistRouteStageProgress()
    {
        if (string.IsNullOrEmpty(_runtimeId)) return;
        s_stageProgressById[_runtimeId] = new RouteStageProgress(_stageIndex, _stageCalled);
    }

    private bool TryGetActiveRoute(out string currentRouteKey, out int currentCallCount, out int currentMaxCalls, out int currentStageIndex)
    {
        currentRouteKey = routeKey;
        currentCallCount = _called;
        currentMaxCalls = Mathf.Max(0, maxCalls);
        currentStageIndex = -1;

        if (!HasRouteStages)
            return currentMaxCalls <= 0 || currentCallCount < currentMaxCalls;

        AdvanceToNextRouteStageIfNeeded();

        currentRouteKey = null;
        currentCallCount = 0;
        currentMaxCalls = 0;

        if (_stageIndex >= routeStages.Count)
            return false;

        var stage = routeStages[_stageIndex];
        if (stage == null || string.IsNullOrWhiteSpace(stage.routeKey))
            return false;

        currentRouteKey = stage.routeKey;
        currentCallCount = _stageCalled;
        currentMaxCalls = Mathf.Max(0, stage.maxCalls);
        currentStageIndex = _stageIndex;
        return currentMaxCalls <= 0 || currentCallCount < currentMaxCalls;
    }

    private void AdvanceToNextRouteStageIfNeeded()
    {
        if (!HasRouteStages) return;

        bool changed = false;

        while (_stageIndex < routeStages.Count)
        {
            var stage = routeStages[_stageIndex];
            if (stage == null || string.IsNullOrWhiteSpace(stage.routeKey))
            {
                if (debugLog)
                    Debug.LogWarning($"[TriggerGet] routeStages[{_stageIndex}] has no routeKey. Skipping.", this);

                _stageIndex++;
                _stageCalled = 0;
                changed = true;
                continue;
            }

            int limit = Mathf.Max(0, stage.maxCalls);
            if (limit > 0 && _stageCalled >= limit)
            {
                if (debugLog)
                    Debug.Log($"[TriggerGet] Route stage complete key='{stage.routeKey}' calls={_stageCalled}/{limit} -> next", this);

                _stageIndex++;
                _stageCalled = 0;
                changed = true;
                continue;
            }

            break;
        }

        if (changed)
            PersistRouteStageProgress();
    }

    private void ApplyConsumedStateIfNeeded()
    {
        if (HasRouteStages)
        {
            AdvanceToNextRouteStageIfNeeded();
            if (_stageIndex < routeStages.Count) return;

            enabled = false;
            if (_selfCollider != null) _selfCollider.enabled = false;

            if (debugLog)
                Debug.Log($"[TriggerGet] Consumed -> disabled routeStages id='{_runtimeId}'", this);
            return;
        }

        if (maxCalls <= 0) return;
        if (_called < maxCalls) return;

        enabled = false;
        if (_selfCollider != null) _selfCollider.enabled = false;

        if (debugLog)
            Debug.Log($"[TriggerGet] Consumed -> disabled key='{routeKey}' id='{_runtimeId}' calls={_called}/{maxCalls}", this);
    }

    private string GetRouteKeyForLog()
    {
        if (TryGetActiveRoute(out string currentRouteKey, out _, out _, out _))
            return currentRouteKey;

        return HasRouteStages ? "(consumed)" : routeKey;
    }

    private string BuildRuntimeId()
    {
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "(no-scene)";
        string routeId = HasRouteStages ? "routeStages" : routeKey;
        return $"{sceneName}::{GetTransformPath(transform)}::{routeId}";
    }

    private static string FormatCallCount(int callCount, int maxCallCount)
    {
        return maxCallCount <= 0 ? $"{callCount}/unlimited" : $"{callCount}/{maxCallCount}";
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "(null)";
        var stack = new Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack.ToArray());
    }
}
