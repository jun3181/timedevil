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
    [SerializeField] private float maxRegularSpawnInterval = 27f;
    [SerializeField] private float minRegularSpawnInterval = 8f;

    [Header("직접적인 순찰병 스폰 최대 딜레이")]
    [SerializeField] private float maxInstantSpawnInterval = 3f;

    [Header("순찰병 스폰시 카메라 경계와 X좌표 차 범위")]
    [SerializeField] private float maxDeltaXWithCameraBound = 8f;
    [SerializeField] private float minDeltaXWithCameraBound = 4f;

    [Header("순찰병 스폰 Y좌표 범위")]
    [SerializeField] private float maxSpawnYPosition = 1.5f;
    [SerializeField] private float minSpawnYPosition = -3f;

    [Header("순찰병 스폰 Y좌표 마진")]
    [SerializeField] private float spawnYMargin = 0.02f;

    private GameObject[] instantiatedTroops;
    private readonly List<GameObject> disappearedTroops = new();

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

        if(spawnYMargin < Physics2D.defaultContactOffset) {
            spawnYMargin = Physics2D.defaultContactOffset;
        }

        player = GameObject.FindWithTag("Player");
        playerRigidbody2D = player.GetComponent<Rigidbody2D>();
        playerCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

        NPCPatroller.OnDisappearing += TroopDisappearingEventHandler;

        GameObject instantiatedTroop = Instantiate(troopPrefab);
        Collider2D troopCollider2D = instantiatedTroop.GetComponent<Collider2D>();

        int n = Mathf.FloorToInt((maxSpawnYPosition - minSpawnYPosition) / (2 * (troopCollider2D.bounds.size.y + spawnYMargin)) + 1);
        laneYSize = troopCollider2D.bounds.size.y + spawnYMargin;
        firstLaneYPosition = (maxSpawnYPosition+minSpawnYPosition)/2 - ((n - 1) * laneYSize) - (troopCollider2D.offset.y*troopCollider2D.transform.localScale.y);
        laneCounter = (n - 1) * 2 + 1;

        instantiatedTroops = new GameObject[laneCounter];
        instantiatedTroops[0] = instantiatedTroop;
        instantiatedTroop.transform.position = new(0, firstLaneYPosition);

        for(int i=1; i<laneCounter; i++) {
            instantiatedTroop = Instantiate(troopPrefab);
            instantiatedTroop.transform.position = new(0, firstLaneYPosition + laneYSize * i);
            instantiatedTroops[i] = instantiatedTroop;
            disappearedTroops.Add(instantiatedTroop);
            
            instantiatedTroop.SetActive(true);
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
        int randomIndex;

        while(true) {
            while(disappearedTroops.Count == 0) {
                yield return null;
            }

            randomIndex = Random.Range(0, disappearedTroops.Count);
            troop = disappearedTroops[randomIndex];
            disappearedTroops.RemoveAt(randomIndex);

            routineInterval = Random.Range(minRegularSpawnInterval, maxRegularSpawnInterval);

            yield return new WaitForSeconds(routineInterval);

            deltaXWithCameraBound = Random.Range(minDeltaXWithCameraBound, maxDeltaXWithCameraBound);
            if(Random.Range(0,2)==0) {
                startPoint.x = playerRigidbody2D.position.x - deltaXWithCameraBound;
            } else {
                startPoint.x = playerRigidbody2D.position.x + deltaXWithCameraBound;
            }
            startPoint.y = troop.transform.position.y;

            troop.SetActive(true);

            troop.transform.position = (startPoint);
            Physics2D.SyncTransforms();

            troop.GetComponent<NPCPatroller>().Move();
            latestSpawnTime = Time.time;
        }
    }

    private void TroopDisappearingEventHandler(int id) {
        disappearedTroops.Add(instantiatedTroops[id]);
    }
}
