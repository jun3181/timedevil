using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NPCTraveller : MonoBehaviour
{
    public enum TravellerMode {
        FullyRandom, // 한번의 순회 동안 동일한 노드를 거칠 수 있음
        Salesman // 한번의 순회 동안 동일한 노드를 거치지 않음
    }

    [SerializeField]
    [Header("Scene 시작시 자동 순회")]
    private bool isAutoStarted = true;

    [SerializeField]
    [Header("순회 모드")]
    private TravellerMode travellerMode = TravellerMode.Salesman;

    [SerializeField]
    [Header("순회점")]
    private List<GameObject> vertexes = new();

    [SerializeField]
    [Header("디버그 메시지 출력")]
    private bool debuged = false;

    // 순회 될 수 있는 후보 정점들
    private HashSet<GameObject> vertexSet = new();

    // 한번의 순회 동안 거칠 정점들
    private Queue<GameObject> circuitNodes = new();

    private IEnumerator travelCoroutine = null;

    void OnEnable() {

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

    private IEnumerator TravelRepeatedly() {
        yield break;
    }
}
