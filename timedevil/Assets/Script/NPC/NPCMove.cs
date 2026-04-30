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

    public bool Moving { get; private set; }

    private Rigidbody2D rb;
    private Collider2D collider;
    private int movementPriority;

    private Vector2 startPos;
    private Vector2 velocity;
    private float takingTime;
    void Awake() {
        movementPriority = ++NPCCount;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        collider = GetComponent<Collider2D>();
    }

    void FixedUpdate() {
        if(Moving) {
            Vector2 newPos = rb.position + Time.fixedDeltaTime * velocity;
            float takenTime = (newPos - startPos).magnitude / Speed;

            Collider2D[] results = new Collider2D[2];
            int newContactCount = Physics2D.OverlapAreaNonAlloc((Vector3)newPos + collider.bounds.extents, (Vector3)newPos - collider.bounds.extents, results);

            if(newContactCount>1) {
                if(CanStandbyForAvoiding) {
                    Idle();
                } else {
                    Stop();
                }
            } else if(takenTime >= takingTime || newPos==rb.position) {
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

        Moving = true;
    }

    // 주어진 좌표로 이동
    public void MoveTo(Vector2 pos) {
        MoveBy(pos - rb.position);
    }

    // NPC 일시정지
    public void Idle() {
        Moving = false;
    }

    // NPC 완전정지
    public void Stop() {
        Moving = false;
        takingTime = 0;
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
    }
}
