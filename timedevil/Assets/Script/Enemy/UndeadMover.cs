using UnityEngine;

/// <summary>
/// 2D Tilemap 환경에서 플레이어를 무조건 따라오는 적 스크립트
/// Rigidbody2D + Collider2D 사용
/// 기존 UndeadMover 호출과 충돌 없이 사용 가능
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class UndeadMover : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform player;

    [Header("이동 설정")]
    public float moveSpeed = 3f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // 2D 평면에서 중력 무시
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // 플레이어 방향 계산
        Vector2 direction = (player.position - transform.position).normalized;

        // Rigidbody2D 이동
        Vector2 newPos = (Vector2)transform.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);

        // 스프라이트 좌우 반전
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    // 기존 코드와 호환을 위해 StartPatrol 함수 유지 (호출해도 영향 없음)
    public void StartPatrol()
    {
        // 순찰 관련 없음
    }
}
