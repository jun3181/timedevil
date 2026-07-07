using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class NPCPatrollerController : MonoBehaviour
{
    public static NPCPatrollerController instance;

    private GameObject player;
    private Rigidbody2D playerRigidbody2D;
    private Camera playerCamera;

    [SerializeField]
    [Header("정찰병 프리팹")]
    private GameObject troopPrefab;

    [Header("주기적인 순찰병 스폰 최소/최대 시간 간격")]
    [Range(0f, 30f)]
    [SerializeField] private float minRegularSpawnInterval = 8f;
    [Range(0f, 30f)]
    [SerializeField] private float maxRegularSpawnInterval = 27f;

    [Header("직접적인 순찰병 스폰 최대 딜레이")]
    [Range(0f, 10f)]
    [SerializeField] private float maxInstantSpawnInterval = 3f;

    [Header("순찰병 스폰시 카메라 경계와 X좌표 차 범위")]
    [SerializeField] private float minDeltaXWithCameraBound = 4f;
    [SerializeField] private float maxDeltaXWithCameraBound = 8f;

    [Header("순찰병 스폰 Y좌표 범위")]
    [SerializeField] private float minSpawnYPosition = -3f;
    [SerializeField] private float maxSpawnYPosition = 1.5f;

    private GameObject[] instantiatedTroops;
    private Queue<GameObject> disappearedTroops = new();

    private float firstLaneYPosition = 0f;
    private float laneYSize = 0f;
    private int laneCounter = 0;
    private float latestSpawnTime;

    private IEnumerator spawningCoroutine;

    void Awake() {
        if(instance!=null || troopPrefab==null) {
            Destroy(gameObject);
        }

        instance = this;

        player = GameObject.FindWithTag("Player");
        playerRigidbody2D = player.GetComponent<Rigidbody2D>();
        playerCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

        NPCPatroller.OnDisappearing += TroopDisappearingEventHandler;

        GameObject instantiatedTroop = Instantiate(troopPrefab);
        Collider2D troopCollider2D = instantiatedTroop.GetComponent<Collider2D>();

        int n = Mathf.FloorToInt((maxSpawnYPosition - minSpawnYPosition) / (2 * (troopCollider2D.bounds.size.y + Physics2D.defaultContactOffset)) + 1);
        laneYSize = troopCollider2D.bounds.size.y + Physics2D.defaultContactOffset;
        firstLaneYPosition = (maxSpawnYPosition+minSpawnYPosition)/2 - ((n - 1) * laneYSize) - (troopCollider2D.offset.y*troopCollider2D.transform.localScale.y);
        laneCounter = (n - 1) * 2 + 1;

        Debug.Log(firstLaneYPosition);

        for(int i=0; i<laneCounter; i++) {
            instantiatedTroop = Instantiate(troopPrefab);
            instantiatedTroop.transform.position = new Vector2(0, firstLaneYPosition + laneYSize * i);
        }
        
        StartSpawningRegularly();
    }

    public bool StartSpawningRegularly() {
        if(spawningCoroutine != null) return false;

        spawningCoroutine = SpawnRegularly();
        StartCoroutine(spawningCoroutine);

        return true;
    }

    public void StopSpawningRegularly() {
        if(spawningCoroutine!=null) {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
    }

    private IEnumerator SpawnRegularly() {
        Vector2 startPoint = new();
        float routineInterval, deltaXWithCameraBound;
        GameObject troop;

        while(true) {
            while(disappearedTroops.Count == 0) {
                yield return null;
            }

            troop = disappearedTroops.Dequeue();
            routineInterval = Random.Range(minRegularSpawnInterval, maxRegularSpawnInterval);

            yield return new WaitForSeconds(routineInterval);

            deltaXWithCameraBound = Random.Range(minDeltaXWithCameraBound, maxDeltaXWithCameraBound);
            if(Random.Range(0,2)==0) {
                startPoint.x = playerRigidbody2D.position.x - deltaXWithCameraBound;
            } else {
                startPoint.x = playerRigidbody2D.position.x + deltaXWithCameraBound;
            }
            startPoint.y = Random.Range(minSpawnYPosition, maxSpawnYPosition);

            troop.SetActive(true);

            troop.transform.position = (startPoint);
            Physics2D.SyncTransforms();

            troop.GetComponent<NPCPatroller>().Move();
            latestSpawnTime = Time.time;
        }
    }

    private void TroopDisappearingEventHandler(int id) {
        disappearedTroops.Enqueue(instantiatedTroops[id]);
    }
}
