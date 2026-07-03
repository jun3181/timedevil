using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMoverTest : NPCMover
{
    public bool btn = true;
    public GameObject target;

    void Start()
    {
        Debug.Log(MoveTo(target.transform.position));
    }

    void Update() {
        if(!btn) Stop();
    }
}
