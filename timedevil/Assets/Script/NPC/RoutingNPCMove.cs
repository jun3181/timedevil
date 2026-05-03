using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(NPCMove))]
public class RoutingNPCMove : MonoBehaviour
{
    [Header("목표 지점의 오브젝트")]
    [SerializeField]
    private GameObject target;

    [Header("경유할 위치의 오브젝트")]
    [SerializeField]
    private List<GameObject> nodes = new();

    [Header("탐색 크기")]
    [SerializeField]
    private float searchSize = 0.5f;

    [Header("디버그 메시지 출력 여부")]
    [SerializeField]
    private bool debuged = false;

    private NPCMove npcMove;
    private AStarPathfinder pathfinder;

    private IEnumerator routingCoroutine;

    private Dictionary<(Vector2, Vector2), List<Vector2Int>> edges = new();

    void OnValidate() {
        if(searchSize<Physics2D.defaultContactOffset) {
            searchSize = Physics2D.defaultContactOffset;
            Debug.LogWarning($"RoutingNPCMove.searchSize는 {Physics2D.defaultContactOffset}이상 이어야 합니다.");
        }

        if(target==null) {
            Debug.LogWarning("타겟을 설정하십시오.");
        }
    }

    void Start() {
        npcMove = GetComponent<NPCMove>();
        npcMove.CanStandbyForAvoiding = true;

        pathfinder = new(npcMove.GetCollider2D(), searchSize);
    }

    public void StartRouting() {
        if(routingCoroutine!=null) return;

        routingCoroutine = RoutingCoroutine();
        StartCoroutine(routingCoroutine);
    }

    public void StopRouting() {
        if(routingCoroutine == null) return;

        StopCoroutine(routingCoroutine);
        routingCoroutine = null;

        npcMove.Stop();
    }

    private IEnumerator RoutingCoroutine() {
        // 0. Shuffle Index Array
        List<int> indexList = Enumerable.Range(0, nodes.Count).ToList();

        for(int i = indexList.Count-1; i>0; i--) {
            int targetIndex = Random.Range(0, i + 1);
            (indexList[i], indexList[targetIndex]) = (indexList[targetIndex], indexList[i]);
        }

        nodes.Add(target);
        indexList.Add(nodes.Count-1);

        int[] indexArray = indexList.ToArray();

        yield return null;

        // 1. Route
        Vector2 currentPos;
        List<Vector2Int> path;
        WaitForSeconds waitForQuart = new(0.25f);
        for(int i=0; i<nodes.Count; i++) {
            currentPos = npcMove.GetPosition();
            Vector2 nextPos = nodes[indexArray[i]].transform.position;

            edges.TryGetValue((currentPos, nextPos), out path);
            if(path==null) {
                path = new List<Vector2Int>();

                StartCoroutine(pathfinder.FindPath(currentPos, nextPos, path));
                while(path.Count==0) {
                    yield return null;
                }

                edges[(currentPos, nextPos)] = path;
            }

            if(path[0]==AStarPathfinder.ERROR_SIGNAL) {
                continue;
            }

            int j = 0;
            while(j<path.Count) {
                if(npcMove.WasOnMoving()) {
                    path = new();
                    j = 0;

                    StartCoroutine(pathfinder.FindPath(npcMove.GetPosition(), nextPos, path));
                    while(path.Count == 0) yield return waitForQuart;

                    currentPos = npcMove.GetPosition();

                    if(path[0] == AStarPathfinder.ERROR_SIGNAL) {
                        break;
                    }
                }
                Vector2Int offset = path[j];

                npcMove.MoveTo(currentPos + (Vector2)offset * searchSize);
                while(npcMove.Moving) {
                    yield return null;
                }

                j++;
            }
        }

        nodes.RemoveAt(nodes.Count-1);

        routingCoroutine = null;
    }
}
