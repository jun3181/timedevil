// Assets/Script/Interactable/TriggerRouterInteraction.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerRouterInteraction : MonoBehaviour, IInteractable
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

    private static readonly Dictionary<string, int> s_callCountById = new();
    private static readonly Dictionary<string, RouteStageProgress> s_stageProgressById = new();
    private static readonly HashSet<string> s_completedById = new();

    [Header("Router (auto-find if empty)")]
    [SerializeField] private TriggerRouter router;

    [Header("Route Stages (Optional)")]
    [Tooltip("If entries exist, stages run in order. Completion Item Condition can override these stages.")]
    [SerializeField] private List<TriggerRouteStage> routeStages = new();

    [Header("Completion Item Condition")]
    [SerializeField] private TriggerItemCompletionCondition completionItemCondition = new();

    [Header("Fallback Route Key")]
    [SerializeField] private string routeKey = "Trigger1";

    [Header("Fallback Call Limit")]
    [Tooltip("0 means unlimited. 1 means one call. 2 means two calls.")]
    [Min(0)]
    [SerializeField] private int maxCalls = 0;

    [Tooltip("When fully consumed, disable this object's Collider2D components.")]
    [SerializeField] private bool disableCollidersWhenConsumed = true;

    [Header("Policy")]
    [SerializeField] private bool blockIfDialogueActive = true;
    [SerializeField] private bool blockIfGameActionLocked = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private int _called;
    private int _stageIndex;
    private int _stageCalled;
    private bool _completed;
    private string _runtimeId;
    private Collider2D[] _colliders;

    private bool HasRouteStages => routeStages != null && routeStages.Count > 0;

    private void Awake()
    {
        if (!router) router = FindObjectOfType<TriggerRouter>(true);

        _runtimeId = BuildRuntimeId();
        _colliders = GetComponents<Collider2D>();

        RestoreProgress();
        ApplyConsumedStateIfNeeded();
    }

    public void Interact()
    {
        if (IsCompletionConsumed())
        {
            ApplyConsumedStateIfNeeded();
            return;
        }

        bool isCompletionRoute = TryGetCompletionRoute(
            out string currentRouteKey,
            out TriggerItemRouteDecision completionDecision);

        int currentCallCount = isCompletionRoute ? 1 : 0;
        int currentMaxCalls = isCompletionRoute ? 1 : 0;
        int currentStageIndex = -1;

        if (!isCompletionRoute && !TryGetActiveRoute(
                out currentRouteKey,
                out currentCallCount,
                out currentMaxCalls,
                out currentStageIndex))
        {
            ApplyConsumedStateIfNeeded();
            return;
        }

        if (string.IsNullOrWhiteSpace(currentRouteKey))
        {
            if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] routeKey is empty.", this);
            return;
        }

        if (!router)
        {
            router = FindObjectOfType<TriggerRouter>(true);
            if (!router)
            {
                if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] TriggerRouter was not found.", this);
                return;
            }
        }

        if (blockIfDialogueActive && DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        if (blockIfGameActionLocked && GameManager.Instance != null && GameManager.Instance.isAction)
            return;

        var pm = FindObjectOfType<PlayerMove>(true);
        if (!pm)
        {
            if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] PlayerMove was not found.", this);
            return;
        }

        var col = pm.GetComponent<Collider2D>();

        var ctx = new TriggerContext(
            trigger: null,
            router: router,
            instigator: pm.gameObject,
            instigatorCollider: col,
            playerMove: pm
        );

        if (isCompletionRoute)
            RegisterCompletion();
        else
            RegisterRouteCall();

        if (!isCompletionRoute)
            currentCallCount++;

        if (isCompletionRoute)
            completionItemCondition.ConsumeRequiredItemsIfNeeded(completionDecision);

        if (debugLog)
        {
            string stageSuffix = currentStageIndex >= 0 ? $" stage={currentStageIndex + 1}/{routeStages.Count}" : "";
            string completionSuffix = isCompletionRoute ? " completion" : "";
            string conditionSuffix = isCompletionRoute ? FormatConditionSuffix(completionDecision) : "";
            Debug.Log($"[TriggerRouterInteraction] RequestRoute key='{currentRouteKey}' call={FormatCallCount(currentCallCount, currentMaxCalls)}{completionSuffix}{stageSuffix}{conditionSuffix} by='{pm.name}'", this);
        }

        router.RequestRoute(currentRouteKey, ctx);
        ApplyConsumedStateIfNeeded();
    }

    private void RestoreProgress()
    {
        _called = 0;
        _stageIndex = 0;
        _stageCalled = 0;
        _completed = false;

        if (string.IsNullOrEmpty(_runtimeId)) return;

        _completed = s_completedById.Contains(_runtimeId);

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

    private bool TryGetCompletionRoute(out string completionRouteKey, out TriggerItemRouteDecision decision)
    {
        completionRouteKey = null;
        decision = TriggerItemRouteDecision.NoCondition();

        if (completionItemCondition == null || !completionItemCondition.IsEnabled)
            return false;

        decision = completionItemCondition.Evaluate();
        if (!decision.isMet)
            return false;

        completionRouteKey = completionItemCondition.CompleteRouteKey;
        if (!string.IsNullOrWhiteSpace(completionRouteKey))
            return true;

        if (debugLog)
            Debug.LogWarning("[TriggerRouterInteraction] Completion item condition is met, but Complete Route Key is empty.", this);

        return false;
    }

    private void RegisterCompletion()
    {
        _completed = true;

        if (!string.IsNullOrEmpty(_runtimeId))
            s_completedById.Add(_runtimeId);
    }

    private bool IsCompletionConfigured()
    {
        return completionItemCondition != null && completionItemCondition.IsEnabled;
    }

    private bool IsCompletionConsumed()
    {
        return _completed || (!string.IsNullOrEmpty(_runtimeId) && s_completedById.Contains(_runtimeId));
    }

    private bool TryGetActiveRoute(
        out string currentRouteKey,
        out int currentCallCount,
        out int currentMaxCalls,
        out int currentStageIndex)
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
                    Debug.LogWarning($"[TriggerRouterInteraction] routeStages[{_stageIndex}] has no routeKey. Skipping.", this);

                _stageIndex++;
                _stageCalled = 0;
                changed = true;
                continue;
            }

            int limit = Mathf.Max(0, stage.maxCalls);
            if (limit > 0 && _stageCalled >= limit)
            {
                if (debugLog)
                    Debug.Log($"[TriggerRouterInteraction] Route stage complete key='{stage.routeKey}' calls={_stageCalled}/{limit} -> next", this);

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
        if (IsCompletionConsumed())
        {
            DisableAsConsumed();

            if (debugLog)
                Debug.Log($"[TriggerRouterInteraction] Consumed -> disabled completion id='{_runtimeId}'", this);

            return;
        }

        if (HasRouteStages)
        {
            AdvanceToNextRouteStageIfNeeded();
            if (_stageIndex < routeStages.Count) return;
            if (IsCompletionConfigured()) return;

            DisableAsConsumed();

            if (debugLog)
                Debug.Log($"[TriggerRouterInteraction] Consumed -> disabled routeStages id='{_runtimeId}'", this);
            return;
        }

        if (maxCalls <= 0) return;
        if (_called < maxCalls) return;
        if (IsCompletionConfigured()) return;

        DisableAsConsumed();

        if (debugLog)
            Debug.Log($"[TriggerRouterInteraction] Consumed -> disabled key='{routeKey}' id='{_runtimeId}' calls={_called}/{maxCalls}", this);
    }

    private void DisableAsConsumed()
    {
        enabled = false;

        if (!disableCollidersWhenConsumed)
            return;

        if (_colliders == null || _colliders.Length == 0)
            _colliders = GetComponents<Collider2D>();

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = false;
        }
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

    private static string FormatConditionSuffix(TriggerItemRouteDecision decision)
    {
        if (!decision.usesCondition)
            return "";

        string state = decision.isMet ? "met" : "not-met";
        return $" itemCondition={state} item='{decision.itemId}' qty={decision.currentQuantity}/{decision.requiredQuantity}";
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
