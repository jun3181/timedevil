using UnityEngine;

/// <summary>
/// Rigidbody2D 기반으로 플레이어를 추적하는 적 이동 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class UndeadMover : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform player;
    [SerializeField] private bool autoFindPlayerOnStartPatrol = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isFollowing;

    public Transform Player => player;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (!isFollowing || player == null)
            return;

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        Vector2 newPos = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);

        if (spriteRenderer != null)
            spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    public void StartPatrol()
    {
        if (player == null && autoFindPlayerOnStartPatrol)
            player = ResolvePlayer();

        isFollowing = player != null;

        if (!isFollowing && rb != null)
            rb.velocity = Vector2.zero;
    }

    public void StopPatrol()
    {
        isFollowing = false;

        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private static Transform ResolvePlayer()
    {
        var playerMove = Object.FindObjectOfType<PlayerMove>(true);
        if (playerMove != null)
            return playerMove.transform;

        var playerAction = Object.FindObjectOfType<PlayerAction>(true);
        return playerAction != null ? playerAction.transform : null;
    }
}
