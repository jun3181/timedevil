using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start");
        StartCoroutine(CoA());
        Debug.Log("End");
    }

    IEnumerator CoA() {
        Debug.Log("a");
        Debug.Log("b");
        yield return CoB();
        Debug.Log("c");
        Debug.Log("d");
        yield break;
    }

    IEnumerator CoB() {
        Debug.Log("가");
        yield break;
    }
}
