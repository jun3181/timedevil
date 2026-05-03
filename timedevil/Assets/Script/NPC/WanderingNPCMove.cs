using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMove))]
public class WanderingNPCMove : MonoBehaviour, INPCMoveController
{
    [Header("움직임 활성화 여부")]
    [SerializeField]
    private bool Wandering = false;

    [Header("최대 이동 쿨타임")]
    [Tooltip("정지 후 다시 움직이는데 까지 최대로 걸리는 시간")]
    public float MaxCooltime = 10f;

    [Header("최소 이동 쿨타임")]
    public float MinCooltime = 3f;

    [Header("최대 이동거리")]
    [Tooltip("한번에 이동할 수 있는 최대 거리")]
    public uint MaxDistance = 5;

    [Header("최소 이동거리")]
    [Tooltip("한번에 이동할 수 있는 최소 거리")]
    public uint MinDistance = 1;

    [Header("디버그 라인 활성화")]
    public bool Debuged = true;

    private readonly Vector2[] directions =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        Vector2.one, -Vector2.down, new(1,-1), new(-1, 1)
    };

    private Rigidbody2D rb;

    private NPCMove npcMove = null;
    private IEnumerator cooltimeCancelCoroutine = null;
    private bool isCooltimeRun = false;
    void OnValidate() {
        if(MinCooltime < 0) {
            if(Debuged) Debug.LogWarning($"{gameObject.name}의 최소 이동 쿨타임이 음수입니다.");
            MinCooltime = 3f;
        }

        if(MaxCooltime < 0) {
            if(Debuged) Debug.LogWarning($"{gameObject.name}의 최대 이동 쿨타임이 음수입니다.");
            MaxCooltime = 10f;
        }

        if(MinDistance>MaxDistance) {
            if(Debuged) Debug.LogWarning($"{gameObject.name}의 최소 이동거리가 최대 이동거리보다 큽니다.");
            (MinDistance, MaxDistance) = (MaxDistance, MinDistance);
        }
    }

    void Start()
    {
        npcMove = GetComponent<NPCMove>();
        npcMove.CanStandbyForAvoiding = false;

        rb = GetComponent<Rigidbody2D>();
    }

    void Update() {
        if(Wandering) {
            if(isCooltimeRun) return;

            // 쿨타임이 끝났거나 최초로 시작되었을 때
            if(!npcMove.WasOnMoving() && cooltimeCancelCoroutine == null) {
                Vector2 direction = directions[Random.Range(0, directions.Length)];

                float distance = Random.Range((int)MinDistance, (int)MaxDistance + 1);

                direction *= distance;
                
                npcMove.MoveBy(direction);
            // OnWandering이 false되어 일시적으로 멈춘 후 다시 시작할 때
            } else if(!npcMove.Moving && cooltimeCancelCoroutine == null) {
                npcMove.Resume();
            }

            // 움직이는 동안 쿨타임 코루틴 설정
            if(npcMove.Moving && cooltimeCancelCoroutine == null) {
                cooltimeCancelCoroutine = CancelCooltimeAfterSeconds(Random.Range(MinCooltime, MaxCooltime));
            // 한번의 움직임이 끝났을 때 쿨타임 적용 시작
            } else if(!npcMove.Moving && !isCooltimeRun) {
                StartCoroutine(cooltimeCancelCoroutine);
            }
        } else {
            // 쿨타임 실행 중 이였다면 정지
            if(cooltimeCancelCoroutine!=null) {
                StopCoroutine(cooltimeCancelCoroutine);
                cooltimeCancelCoroutine = null;
                isCooltimeRun = false;
            }

            // 움직이고 있었다면 일시정지
            if(npcMove.Moving) {
                npcMove.Idle();
            }
        }
    }

    private IEnumerator CancelCooltimeAfterSeconds(float sec) {
        isCooltimeRun = true;
        yield return new WaitForSeconds(sec);

        cooltimeCancelCoroutine = null;
        isCooltimeRun = false;
        yield break;
    }

    public void Idle() {
        Wandering = false;
    }

    public void Resume() {
        Wandering = true;
    }
}
