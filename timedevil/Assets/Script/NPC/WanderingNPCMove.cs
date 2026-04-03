using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMove))]
public class WanderingNPCMove : MonoBehaviour
{
    [Header("돌아다니는 기능 활성화")]
    public bool OnWandering = false;

    [Header("이동 쿨타임")]
    [Tooltip("정지 후 다시 움직이는데 까지 걸리는 시간")]
    public float Cooltime = 3f;

    [Header("디버그 라인 활성화")]
    public bool Debuged = true;

    private Vector2[] directions =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private NPCMove npcMove = null;
    private IEnumerator cooltimeCancelCoroutine = null;
    private bool isCooltimeRun = false;
    void Awake() {
        if(Cooltime < 0) {
            if(Debuged) Debug.LogWarning($"{gameObject.name}의 이동 쿨타임이 음수입니다.");
            Cooltime = 0f;
        }
    }

    void Start()
    {
        npcMove = GetComponent<NPCMove>();
    }

    void Update() {
        if(OnWandering) {
            if(!npcMove.Moving && cooltimeCancelCoroutine==null) {
                Vector2 direction = directions[Random.Range(0, directions.Length)];
                int magnitude = Random.Range(1, 6);

                direction *= magnitude;

                npcMove.MoveBy(direction);
            } else if(npcMove.Moving && cooltimeCancelCoroutine==null) {
                cooltimeCancelCoroutine = CancelCooltimeAfterSeconds(3);
            } else if(!npcMove.Moving && !isCooltimeRun) {
                StartCoroutine(cooltimeCancelCoroutine);
            }
        } else {
            if(cooltimeCancelCoroutine!=null) {
                StopCoroutine(cooltimeCancelCoroutine);
                cooltimeCancelCoroutine = null;
            }

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
}
