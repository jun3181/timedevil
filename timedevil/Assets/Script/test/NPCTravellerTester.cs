using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTravellerTester : MonoBehaviour
{

    public bool A = true;
    private NPCTraveller npcTraveller;
    void Start() {
        npcTraveller = GetComponent<NPCTraveller>();
    }

    void Update()
    {
        if(!A)
            npcTraveller.Stop();
        else
            npcTraveller.Travel();
    }
}
