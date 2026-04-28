using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxCastTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Collider2D collider2d = GetComponent<Collider2D>();
        RaycastHit2D[] reses = new RaycastHit2D[1];
        int count = collider2d.Cast(Vector2.down, reses, 1);

        Debug.Log(count);
        Debug.Log(0.999f==0f);

        foreach(RaycastHit2D res in reses) {
            Debug.Log(res.transform);
        }
    }
}
