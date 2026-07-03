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
    private int lastHAxisRaw;
    private int lastVAxisRaw = -1;


    [Header("Debug")]
    [SerializeField] private bool debugAnimatorTrace = false;
    [SerializeField] private float debugTraceInterval = 0.1f;

    private float nextDebugTraceAt;

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
            bool hasMoveInput = this.h != 0 || this.v != 0;

            if (hasMoveInput)
            {
                if (isHorizonMove && this.h != 0)
                {
                    lastHAxisRaw = this.h;
                    lastVAxisRaw = 0;
                }
                else if (!isHorizonMove && this.v != 0)
                {
                    lastHAxisRaw = 0;
                    lastVAxisRaw = this.v;
                }
            }

            if (anim.GetInteger("hAxisRaw") != lastHAxisRaw)
                anim.SetInteger("hAxisRaw", lastHAxisRaw);

            if (anim.GetInteger("vAxisRaw") != lastVAxisRaw)
                anim.SetInteger("vAxisRaw", lastVAxisRaw);

            anim.SetBool("isChange", hasMoveInput);

            if (debugAnimatorTrace && Time.unscaledTime >= nextDebugTraceAt)
            {
                nextDebugTraceAt = Time.unscaledTime + Mathf.Max(0.01f, debugTraceInterval);
                LogAnimatorTrace(hDown, vDown, hUp, vUp, hasMoveInput);
            }
        }

        // 바라보는 방향
        if (hDown || (this.h != 0 && isHorizonMove))
            facing = (this.h > 0) ? Vector3.right : Vector3.left;
        else if (vDown || (this.v != 0 && !isHorizonMove))
            facing = (this.v > 0) ? Vector3.up : Vector3.down;
    }

    public void ChangeSpeed(float speed) {
        this.speed = speed;
    }


    private void LogAnimatorTrace(bool hDown, bool vDown, bool hUp, bool vUp, bool hasMoveInput)
    {
        if (!anim) return;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        Debug.Log(
            $"[PlayerMove][AnimTrace] t={Time.unscaledTime:F3}, " +
            $"input(h={h},v={v},hD={hDown},vD={vDown},hU={hUp},vU={vUp}), " +
            $"param(hAxisRaw={anim.GetInteger("hAxisRaw")},vAxisRaw={anim.GetInteger("vAxisRaw")},isChange={anim.GetBool("isChange")},hasMoveInput={hasMoveInput}), " +
            $"state(hash={state.shortNameHash},norm={state.normalizedTime:F3},loop={state.loop}," +
            $"LWalk={state.IsName("Player_Left_Walk")},LIdle={state.IsName("Player_Left_Idle")}," +
            $"RWalk={state.IsName("Player_Right_Walk")},RIdle={state.IsName("Player_Right_Idle")}," +
            $"UWalk={state.IsName("Player_Up_Walk")},UIdle={state.IsName("Player_Up_Idle")}," +
            $"DWalk={state.IsName("Player_Down_Walk")},DIdle={state.IsName("Player_Down_Idle")}), " +
            $"velocity=({rb.velocity.x:F3},{rb.velocity.y:F3}), " +
            $"controller={(anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "null")}, " +
            $"driveAnimator={driveAnimator}"
        );
    }

    private void FixedUpdate()
    {
        Vector2 input = new Vector2(h, v);
        rb.velocity = input.sqrMagnitude > 0f ? input.normalized * speed : Vector2.zero;
    }
}
