using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTraveller : NPCMover
{
    public enum NPCTravellerMode {
        FullyRandom, // 한번의 순회 동안 동일한 노드를 거칠 수 있음
        Salesman // 한번의 순회 동안 동일한 노드를 거치지 않음
    }

    [SerializeField]
    [Header("Scene 시작시 자동 순회")]
    private bool isAutoStarted = true;

    [SerializeField]
    [Header("순회 모드")]
    private NPCTravellerMode travellerMode = NPCTravellerMode.Salesman;

    [SerializeField]
    [Header("초기 순회점")]
    private List<GameObject> initialVertexes = new();

    [SerializeField]
    [Header("디버그 메시지 출력")]
    private bool debuged = false;

    // 순회 될 수 있는 후보 정점들
    private HashSet<GameObject> vertexSet = new();

    // 한번의 순회 동안 거칠 정점들
    private Queue<GameObject> circuitNodes = new();

    private IEnumerator travelCoroutine = null;

    protected override void Awake() {
        base.Awake();
        for(int i=0; i<initialVertexes.Count; i++) {
            if(initialVertexes[i] == null) {
                initialVertexes.RemoveAt(i);
                i--;
            }
        }
    }

    void OnEnable() {
        vertexSet = new(initialVertexes);

        if(vertexSet.Count < 2) {
            if(debuged) Debug.LogError($"{gameObject.name}.TravellingNPC의 유효한 순회 정점이 2개 미만입니다.\n정점 수: {vertexSet.Count}");
            enabled = false;
        }

        if(enabled && isAutoStarted) {
            Travel();
        }
    }
    
    // 순회 시작(혹은, 재개)
    public bool Travel() {
        if(!enabled || travelCoroutine!=null) return false;

        travelCoroutine = TravelRepeatedly();
        StartCoroutine(travelCoroutine);

        return true;
    }

    // 순회 완전 정지(재개X)
    public void Stop() {
        Idle();

        circuitNodes.Clear();
        circuitNodes.TrimExcess();
    }

    // 순회 일시 정지(재개 가능)
    public void Idle() {
        StopCoroutine(travelCoroutine);

        travelCoroutine = null;
    }

    public bool AddVertex(GameObject vertex) {
        if(travelCoroutine != null) return false;

        return vertexSet.Add(vertex);
    }

    public bool RemoveVertex(GameObject vertex) {
        if(travelCoroutine != null) return false;

        return vertexSet.Remove(vertex);
    }

    public bool RemoveVertex(string name) {
        if(travelCoroutine != null) return false;

        foreach(var vertex in vertexSet) {
            if(vertex.name == name)
                return vertexSet.Remove(vertex);
        }

        return false;
    }

    private void ResetCircuitNodes() {
        if(travellerMode == NPCTravellerMode.FullyRandom) {
            List<GameObject> shuffled_nodes = new();
            int ex_vertex_i, vertex_i;

            // Work in Progress
        } else if(travellerMode == NPCTravellerMode.Salesman) {
            List<GameObject> shuffled_nodes = new();
            int i;

            foreach(var vertex in vertexSet) {
                i = Random.Range(0, shuffled_nodes.Count + 1);
                if(i != shuffled_nodes.Count) {
                    shuffled_nodes.Insert(i, vertex);
                } else {
                    shuffled_nodes.Add(vertex);
                }
            }

            circuitNodes = new Queue<GameObject>(shuffled_nodes);
        }

    }

    private IEnumerator TravelRepeatedly() {
        GameObject nextNode;
        while(true) {
            if(circuitNodes.Count==0) {
                ResetCircuitNodes();
            }
            nextNode = circuitNodes.Dequeue();
        }
    }
}
