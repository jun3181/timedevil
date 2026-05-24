using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 3f;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;

    [Header("Animator Drive")]
    [SerializeField] private bool driveAnimator = true;

    private int h;
    private int v;
    private bool isHorizonMove;

    private Vector3 facing = Vector3.down;
    public Vector3 Facing => facing;

    private void Reset()
    {
        rb ??= GetComponent<Rigidbody2D>();
        anim ??= GetComponent<Animator>();
    }

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();

        // PlayerAction이 활성화된 씬에서는 해당 스크립트가 Animator 파라미터를 이미 제어한다.
        // 중복 제어로 isChange가 덮어써지는 것을 막기 위해 PlayerMove 쪽 구동을 자동 비활성화한다.
        if (driveAnimator && TryGetComponent<PlayerAction>(out var playerAction) && playerAction.enabled)
            driveAnimator = false;
    }

    public void SetMoveInput(int h, int v, bool hDown, bool vDown, bool hUp, bool vUp)
    {
        this.h = Mathf.Clamp(h, -1, 1);
        this.v = Mathf.Clamp(v, -1, 1);

        if (driveAnimator && anim)
        {
            if (hDown) isHorizonMove = true;
            else if (vDown) isHorizonMove = false;
            else if (hUp || vUp) isHorizonMove = this.h != 0;

            // 애니 파라미터 갱신
            if (anim.GetInteger("hAxisRaw") != this.h)
                anim.SetInteger("hAxisRaw", this.h);

            if (anim.GetInteger("vAxisRaw") != this.v)
                anim.SetInteger("vAxisRaw", this.v);

            bool hasMoveInput = this.h != 0 || this.v != 0;
            anim.SetBool("isChange", hasMoveInput);
        }

        // 바라보는 방향
        if (hDown || (this.h != 0 && isHorizonMove))
            facing = (this.h > 0) ? Vector3.right : Vector3.left;
        else if (vDown || (this.v != 0 && !isHorizonMove))
            facing = (this.v > 0) ? Vector3.up : Vector3.down;
    }

    private void FixedUpdate()
    {
        Vector2 input = new Vector2(h, v);
        rb.velocity = input.sqrMagnitude > 0f ? input.normalized * speed : Vector2.zero;

        if (driveAnimator && anim)
        {
            bool isActuallyMoving = rb.velocity.sqrMagnitude > 0.0001f;
            anim.SetBool("isChange", isActuallyMoving);
        }
    }
}
