using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NPCMove : MonoBehaviour
{
    private static int NPCCount = 0;

    [Header("NPC 이동 속력")]
    public float Speed = 1f;

    [Header("전방에 장애물 존재 시 일시정지")]
    [Tooltip("거짓일 경우 장애물과 만날 시 해당 지점에서 완전 정지하며 참일 경우 장애물이 없어질 때까지 일시정지")]
    public bool CanStandbyForAvoiding = false;

    [Header("디버그 메시지 출력 여부")]
    [SerializeField]
    private bool debuged = true;

    [Header("Animator Drive")]
    [SerializeField] private bool driveAnimator = true;
    [SerializeField] private Animator anim;
    [SerializeField] private bool strictAnimatorParamCheck = true;
    [SerializeField] private string paramIsChange = "isChange";
    [SerializeField] private string paramHAxisRaw = "hAxisRaw";
    [SerializeField] private string paramVAxisRaw = "vAxisRaw";

    public bool Moving { get; private set; }

    private Rigidbody2D rb;
    private Collider2D collider;
    private int movementPriority;

    private Vector2 startPos;
    private Vector2 velocity;
    private float takingTime;
    private int lastHAxisRaw;
    private int lastVAxisRaw = -1;
    private bool canDriveAnimator;

    private Vector2 colliderGlobalOffset = new();
    // private Vector2 collisionDetectionPadding = new(0.02f, 0.02f);
    /* 이동할 영역에 콜라이더가 있으면 NPC는 정지하는데,
     * 콜라이더를 감지할 영역을 축소하는 역할을 함
     * 패딩을 적용할 경우 플레이어가 옆에서 부딪힐 때 정지하지 않음
     */

    void Awake() {
        movementPriority = ++NPCCount;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        collider = GetComponent<Collider2D>();
        if(!anim) anim = GetComponent<Animator>();
        canDriveAnimator = driveAnimator && HasRequiredAnimatorParams();
        if(driveAnimator && !canDriveAnimator && debuged) {
            Debug.LogWarning($"{gameObject.name}의 Animator 파라미터가 올바르지 않아 NPCMove Animator 구동을 건너뜁니다.");
        }
        ApplyMoveAnimation(false);
        
        colliderGlobalOffset.x = collider.offset.x * transform.localScale.x;
        colliderGlobalOffset.y = collider.offset.y * transform.localScale.y;
    }

    void FixedUpdate() {
        if(Moving) {
            Vector2 newPos = rb.position + Time.fixedDeltaTime * velocity;
            float takenTime = (newPos - startPos).magnitude / Speed;

            Collider2D[] results = new Collider2D[2];
            Vector2 newColliderCenter = newPos + colliderGlobalOffset;
            int newContactCount = Physics2D.OverlapAreaNonAlloc(newColliderCenter + (Vector2)collider.bounds.extents, newColliderCenter - (Vector2)collider.bounds.extents, results);
            if(debuged) Debug.DrawLine(newColliderCenter + (Vector2)collider.bounds.extents, newColliderCenter - (Vector2)collider.bounds.extents, Color.cyan);

            if(newContactCount > 1) {
                if(debuged) Debug.Log($"{gameObject.name}이 {GetPosition()}위치에서 충돌을 회피하기 위해 움직임을 멈춤");
                if(CanStandbyForAvoiding) {
                    Idle();
                } else {
                    Stop();
                }
            } else if(takenTime >= takingTime || newPos == rb.position) {
                rb.MovePosition(startPos + velocity * takingTime);
                Stop();
            } else {
                rb.MovePosition(newPos);
            }
        }
    }

    public void SetMovementPriority(int priority) {
        movementPriority = priority;
    }

    // 주어진 좌표 만큼 이동
    public void MoveBy(Vector2 offset) {
        if(Moving) return;

        startPos = rb.position;
        velocity = offset.normalized * Speed;
        takingTime = offset.magnitude / Speed;

        UpdateLastDirectionFromVelocity();
        Moving = true;
        ApplyMoveAnimation(true);
    }

    // 주어진 좌표로 이동
    public void MoveTo(Vector2 pos) {
        MoveBy(pos - rb.position);
    }

    // NPC 일시정지
    public void Idle() {
        Moving = false;
        ApplyMoveAnimation(false);
    }

    // NPC 완전정지
    public void Stop() {
        Moving = false;
        takingTime = 0;
        ApplyMoveAnimation(false);
    }

    public bool OverlapingColliderExist() {
        Collider2D[] results = new Collider2D[1];
        ContactFilter2D filter = new();
        int contactCount = rb.OverlapCollider(filter.NoFilter(), results);
        if(contactCount==0) {
            return false;
        } else {
            return true;
        }
    }

    public bool WasOnMoving() {
        if(!Moving && takingTime!=0) {
            return true;
        } else {
            return false;
        }
    }

    // NPC 움직임 재게
    public void Resume() {
        if(Moving || takingTime==0) return;
        Moving = true;
        ApplyMoveAnimation(true);
    }


    private void UpdateLastDirectionFromVelocity() {
        if(velocity.sqrMagnitude <= 0.0001f) return;

        if(Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y)) {
            lastHAxisRaw = velocity.x >= 0f ? 1 : -1;
            lastVAxisRaw = 0;
        } else {
            lastHAxisRaw = 0;
            lastVAxisRaw = velocity.y >= 0f ? 1 : -1;
        }
    }

    private void ApplyMoveAnimation(bool isChange) {
        if(!canDriveAnimator || !anim) return;

        anim.SetInteger(paramHAxisRaw, lastHAxisRaw);
        anim.SetInteger(paramVAxisRaw, lastVAxisRaw);
        anim.SetBool(paramIsChange, isChange);
    }

    private bool HasRequiredAnimatorParams() {
        if(!driveAnimator) return false;
        if(!anim) return false;
        if(!strictAnimatorParamCheck) return true;

        bool hasChange = false;
        bool hasH = false;
        bool hasV = false;

        var pars = anim.parameters;
        for(int i=0; i<pars.Length; i++) {
            var p = pars[i];
            if(p.name == paramIsChange && p.type == AnimatorControllerParameterType.Bool) hasChange = true;
            else if(p.name == paramHAxisRaw && p.type == AnimatorControllerParameterType.Int) hasH = true;
            else if(p.name == paramVAxisRaw && p.type == AnimatorControllerParameterType.Int) hasV = true;
        }

        return hasChange && hasH && hasV;
    }

    public Vector2 GetPosition() {
        return transform.position;
    }

    public Collider2D GetCollider2D() {
        return collider;
    }
}
