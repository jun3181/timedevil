using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMoveTest : MonoBehaviour
{
    private NPCMove npcMove;
    void Start()
    {
        npcMove = GetComponent<NPCMove>();
        npcMove.MoveTo(new Vector2(5, 0));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
