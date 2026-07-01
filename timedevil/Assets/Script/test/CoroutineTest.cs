using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
    public bool a = true;

    private IEnumerator rb;
    private IEnumerator ra;
    void Start()
    {
        ra = CoA();
        rb = CoB();
        StartCoroutine(ra);
    }

    void Update() {
        if(!a) StopCoroutine(rb);
    }

    IEnumerator CoA() {
        for(int i=0; i<5; i++) {
            Debug.Log($"CoA: {i}");
            yield return rb;
        }
    }

    IEnumerator CoB() {
        for(int i=0; i<5; i++) {
            Debug.Log($"CoB: {i}");
            yield return new WaitForSeconds(1f);
        }
    }
}
