using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarTester : MonoBehaviour
{
    private Collider2D collider;
    private NPCMove npcMove;
    private Stack<Vector2Int> result = new();
    private bool blocked = false;
    private Vector2 originPos;

    public GameObject b;
    void Start() {

        collider = GetComponent<Collider2D>();
        npcMove = GetComponent<NPCMove>();

        AStarPathfinder pathfinder = new(collider, 0.1f);

        IEnumerator co = pathfinder.FindPath(collider.bounds.center,b.transform.position, result);

        originPos = collider.bounds.center;
        StartCoroutine(co);
    }

    void Update() {
        if(result.Count != 0 && !blocked) 
            StartCoroutine(Move());
    }

    private IEnumerator Move() {
        blocked = true;
        while(result.Count!=0) {
            Vector2 a = (Vector2)result.Pop() * 0.1f + originPos;
            Debug.Log(a);
            npcMove.MoveTo(a);
            yield return new WaitForSeconds(1);
        }
    }
}
