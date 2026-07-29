using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMoverTest : MonoBehaviour
{
    public bool btn = true;
    private NPCMover npcMover;

    void Start()
    {
        npcMover = GetComponent<NPCMover>();
    }

    void Update() {
        if(!btn)
            npcMover.Speed = 10f;
        else
            npcMover.Speed = 3f;
    }
}
