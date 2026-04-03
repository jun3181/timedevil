using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMove))]
public class WanderingNPCMove : MonoBehaviour
{
    [Header("")]
    public bool OnWandering = false;

    private NPCMove npcMove = null;
    private IEnumerator cooltimeCancelCoroutine = null;
    void Start()
    {
        npcMove = GetComponent<NPCMove>();
    }

    void Update() {
        if(OnWandering) {
            if(!npcMove.Moving && cooltimeCancelCoroutine==null) {
                float x = Random.Range(-1f, 1f);
                float y = Random.Range(-1f, 1f);
                Vector2 pos = new Vector2(x, y);

                npcMove.MoveBy(pos);
            } else if(npcMove.Moving && cooltimeCancelCoroutine==null) {
                cooltimeCancelCoroutine = CancelCooltimeAfterSeconds(3);
                StartCoroutine(cooltimeCancelCoroutine);
            }
        } else {
            if(cooltimeCancelCoroutine!=null) {
                StopCoroutine(cooltimeCancelCoroutine);
                cooltimeCancelCoroutine = null;
            }
        }
    }

    private IEnumerator CancelCooltimeAfterSeconds(float sec) {
        yield return new WaitForSeconds(sec);

        cooltimeCancelCoroutine = null;
        yield break;
    }
}
