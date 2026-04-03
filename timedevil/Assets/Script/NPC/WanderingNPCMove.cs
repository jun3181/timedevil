using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMove))]
public class WanderingNPCMove : MonoBehaviour
{
    NPCMove npcMove;
    void Start()
    {
        npcMove = GetComponent<NPCMove>();
    }

    void FixedUpdate() {
        if(!npcMove.Moving) {
            float x = Random.Range(-1, 1);
            float y = Random.Range(-1, 1);
            Vector2 pos = new Vector2(x, y);

            Debug.Log(pos);

            npcMove.MoveTo(pos);
        }

        //Debug.Log(npcMove.Moving);
    }
}
