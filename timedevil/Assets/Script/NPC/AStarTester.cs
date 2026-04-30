using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarTester : MonoBehaviour
{
    private Collider2D collider;
    private NPCMove npcMove;
    private List<Vector2Int> pathOffsets;
    private Stack<Vector2Int> result = new();
    private System.Diagnostics.Stopwatch watch = new();
    void Start() {
        System.Diagnostics.Stopwatch watch = new();

        collider = GetComponent<Collider2D>();
        npcMove = GetComponent<NPCMove>();

        AStarPathfinder pathfinder = new(collider, 1f);

        IEnumerator co = pathfinder.FindPath(collider.bounds.center, new Vector2(-5, 3), result);

        watch.Start();
        StartCoroutine(co);
    }

    void Update() {
        if(result!=null && result.Count!=0) {
            watch.Stop();
            Debug.Log(watch.ElapsedMilliseconds + "ms");
            result = null;
        }
    }
}
