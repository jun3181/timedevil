// Assets/Script/Interactable/TriggerRouterInteraction.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerRouterInteraction : MonoBehaviour, IInteractable
{
    private static readonly Dictionary<string, int> s_callCountById = new();

    [Header("Router (���� ������ �ڵ� Ž��)")]
    [SerializeField] private TriggerRouter router;

    [Header("Route Key (�ʼ�)")]
    [SerializeField] private string routeKey = "Trigger1";

    [Header("Call Limit")]
    [Tooltip("0이면 무제한, 1이면 1회만, 2면 2회까지만 실행")]
    [SerializeField] private int maxCalls = 0;

    [Tooltip("maxCalls에 도달하면 이 오브젝트의 Collider2D들을 비활성화합니다.")]
    [SerializeField] private bool disableCollidersWhenConsumed = true;

    [Header("Policy")]
    [SerializeField] private bool blockIfDialogueActive = true;
    [SerializeField] private bool blockIfGameActionLocked = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private int _called;
    private string _runtimeId;
    private Collider2D[] _colliders;

    private void Awake()
    {
        if (!router) router = FindObjectOfType<TriggerRouter>(true);

        _runtimeId = BuildRuntimeId();
        _colliders = GetComponents<Collider2D>();

        if (!string.IsNullOrEmpty(_runtimeId) && s_callCountById.TryGetValue(_runtimeId, out int persisted))
            _called = Mathf.Max(0, persisted);

        ApplyConsumedStateIfNeeded();
    }

    public void Interact()
    {
        if (maxCalls > 0 && _called >= maxCalls)
            return;

        if (string.IsNullOrWhiteSpace(routeKey))
        {
            if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] routeKey�� ����ֽ��ϴ�.", this);
            return;
        }

        if (!router)
        {
            router = FindObjectOfType<TriggerRouter>(true);
            if (!router)
            {
                if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] TriggerRouter�� ã�� ���߽��ϴ�.", this);
                return;
            }
        }

        if (blockIfDialogueActive && DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        if (blockIfGameActionLocked && GameManager.Instance != null && GameManager.Instance.isAction)
            return;

        // PlayerMove ������� TriggerContext ����
        var pm = FindObjectOfType<PlayerMove>(true);
        if (!pm)
        {
            if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] PlayerMove�� ã�� ���߽��ϴ�.", this);
            return;
        }

        var col = pm.GetComponent<Collider2D>(); // ��� ctx���� null�� ���� ��

        var ctx = new TriggerContext(
            trigger: null,                 // TriggerGet ����� �ƴ϶� null
            router: router,
            instigator: pm.gameObject,     // ��ȣ�ۿ� ��ü = �÷��̾�
            instigatorCollider: col,
            playerMove: pm
        );

        if (debugLog)
            Debug.Log($"[TriggerRouterInteraction] RequestRoute key='{routeKey}' by='{pm.name}'", this);

        _called++;
        PersistCallCount();

        router.RequestRoute(routeKey, ctx);
        ApplyConsumedStateIfNeeded();
    }

    private void PersistCallCount()
    {
        if (string.IsNullOrEmpty(_runtimeId)) return;
        s_callCountById[_runtimeId] = _called;
    }

    private void ApplyConsumedStateIfNeeded()
    {
        if (maxCalls <= 0) return;
        if (_called < maxCalls) return;

        enabled = false;

        if (disableCollidersWhenConsumed)
        {
            if (_colliders == null || _colliders.Length == 0)
                _colliders = GetComponents<Collider2D>();

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    _colliders[i].enabled = false;
            }
        }

        if (debugLog)
            Debug.Log($"[TriggerRouterInteraction] Consumed -> disabled key='{routeKey}' id='{_runtimeId}' calls={_called}/{maxCalls}", this);
    }

    private string BuildRuntimeId()
    {
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "(no-scene)";
        return $"{sceneName}::{GetTransformPath(transform)}::{routeKey}";
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
