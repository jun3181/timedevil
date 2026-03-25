using UnityEngine;

/// <summary>
/// Simple 2D mover that can chase a target transform.
/// The chase can be toggled by TriggerStep_MonsterMover.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MonsterMover : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private bool chaseOnAwake = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isChasing;

    public Transform Target => target;
    public bool IsChasing => isChasing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponent<SpriteRenderer>();
        isChasing = chaseOnAwake;
    }

    private void FixedUpdate()
    {
        if (!isChasing || target == null) return;

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        Vector2 next = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(next);

        if (spriteRenderer != null)
            spriteRenderer.flipX = target.position.x < transform.position.x;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (debugLog)
            Debug.Log($"[MonsterMover] {name} target -> {(target ? target.name : "null")}");
    }

    public void StartChase(Transform newTarget = null)
    {
        if (newTarget != null)
            target = newTarget;

        if (target == null)
        {
            if (debugLog)
                Debug.LogWarning($"[MonsterMover] {name} StartChase ignored: target is null.");
            return;
        }

        isChasing = true;
    }

    public void StopChase()
    {
        isChasing = false;
        if (rb != null)
            rb.velocity = Vector2.zero;
    }
}
