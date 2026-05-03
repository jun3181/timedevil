using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    RoutingNPCMove rnm;
    void Start()
    {
        rnm = GetComponent<RoutingNPCMove>();
        rnm.StartRouting();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
