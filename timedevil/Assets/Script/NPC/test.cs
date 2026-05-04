using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public bool Switch = true;

    RoutingNPCMove rnm;
    void Start()
    {
        rnm = GetComponent<RoutingNPCMove>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!Switch) {
            rnm.StopRouting();
        } else {
            rnm.StartRouting();
        }
    }
}
