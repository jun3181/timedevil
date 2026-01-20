using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 3f;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;

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
    }

    public void SetMoveInput(int h, int v, bool hDown, bool vDown, bool hUp, bool vUp)
    {
        this.h = Mathf.Clamp(h, -1, 1);
        this.v = Mathf.Clamp(v, -1, 1);

        // 가로/세로 우선 로직
        if (hDown) isHorizonMove = true;
        else if (vDown) isHorizonMove = false;
        else if (hUp || vUp) isHorizonMove = this.h != 0;

        // 애니 파라미터 갱신
        if (anim)
        {
            if (anim.GetInteger("hAxisRaw") != this.h)
            {
                anim.SetBool("isChange", true);
                anim.SetInteger("hAxisRaw", this.h);
            }
            else if (anim.GetInteger("vAxisRaw") != this.v)
            {
                anim.SetBool("isChange", true);
                anim.SetInteger("vAxisRaw", this.v);
            }
            else
            {
                anim.SetBool("isChange", false);
            }
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
    }
}
