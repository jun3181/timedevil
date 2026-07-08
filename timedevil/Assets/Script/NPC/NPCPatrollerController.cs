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
    [SerializeField] private float maxDirectSpawnInterval = 3f;

    [Header("순찰병 스폰시 카메라 경계와 X좌표 차 범위")]
    [SerializeField] private float maxDeltaXWithCameraBound = 8f;
    [SerializeField] private float minDeltaXWithCameraBound = 4f;

    [Header("순찰병 스폰 Y좌표 범위")]
    [SerializeField] private float maxSpawnYPosition = 1.5f;
    [SerializeField] private float minSpawnYPosition = -3f;

    [Header("순찰병 스폰 Y좌표 마진")]
    [SerializeField] private float spawnYMargin = 0.02f;

    private GameObject[] instantiatedTroops;
    private Rigidbody2D[] instantiatedTroopRigidbody2Ds;
    private GameObject[] instantiatedTroopPlayerDetectors;
    private readonly List<GameObject> disappearedTroops = new();
    private GameObject latestPopedTroop;

    private float firstLaneYPosition = 0f;
    private float laneYSize = 0f;
    private int laneCounter = 0;
    private float latestSpawnTime;

    private IEnumerator regularlySpawningCoroutine;

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
        instantiatedTroopRigidbody2Ds = new Rigidbody2D[laneCounter];
        instantiatedTroopPlayerDetectors = new GameObject[laneCounter];

        instantiatedTroops[0] = instantiatedTroop;
        instantiatedTroopRigidbody2Ds[0] = instantiatedTroop.GetComponent<Rigidbody2D>();
        instantiatedTroopPlayerDetectors[0] = instantiatedTroop.transform.Find("PlayerDetector").gameObject;
        instantiatedTroop.transform.position = new(0, firstLaneYPosition);
        disappearedTroops.Add(instantiatedTroop);
        instantiatedTroop.SetActive(false);

        for(int i=1; i<laneCounter; i++) {
            instantiatedTroop = Instantiate(troopPrefab);
            instantiatedTroop.transform.position = new(0, firstLaneYPosition + laneYSize * i);
            instantiatedTroops[i] = instantiatedTroop;
            instantiatedTroopRigidbody2Ds[i] = instantiatedTroop.GetComponent<Rigidbody2D>();
            instantiatedTroopPlayerDetectors[i] = instantiatedTroop.transform.Find("PlayerDetector").gameObject;
            disappearedTroops.Add(instantiatedTroop);
            
            instantiatedTroop.SetActive(false);
        }
    }

    public bool StartSpawningRegularly() {
        if(regularlySpawningCoroutine != null) return false;

        regularlySpawningCoroutine = SpawnRegularly();
        StartCoroutine(regularlySpawningCoroutine);

        return true;
    }

    public void StopSpawningRegularly() {
        if(regularlySpawningCoroutine!=null) {
            StopCoroutine(regularlySpawningCoroutine);
            regularlySpawningCoroutine = null;
        }
    }

    public bool SpawnDirectly() {
        if(disappearedTroops.Count == 0) return false;

        GameObject troop;
        Vector2 troopVelocity;
        Vector2 troopPosition;
        for(int i=0; i<instantiatedTroops.Length; i++) {
            troop = instantiatedTroops[i];
            troopVelocity = instantiatedTroopRigidbody2Ds[i].velocity;
            troopPosition = instantiatedTroopRigidbody2Ds[i].position;
            // 플래이어의 방향으로 이동하고 있는 정찰병이 있다면 리턴
            if(troop.activeSelf && troopVelocity.x*(playerRigidbody2D.position.x-troopPosition.x)>0) {
                return false;
            }
        }

        StopSpawningRegularly();

        int randomIndex = Random.Range(0, disappearedTroops.Count);
        troop = disappearedTroops[randomIndex];
        disappearedTroops.RemoveAt(randomIndex);

        Collider2D cd2d = troop.transform.Find("PlayerDetector").gameObject.GetComponent<Collider2D>();

        float routineInterval = Random.Range(0, maxDirectSpawnInterval);
        StartCoroutine(DelaySpawn(troop, cd2d, routineInterval));

        return true;
    }

    public new void StopAllCoroutines() {
        base.StopAllCoroutines();
    }

    public void IdleAllTroops() {
        foreach(var troop in instantiatedTroops) {
            if(troop.activeSelf) {
                troop.GetComponent<NPCPatroller>().Idle();
            }
        }
    }

    public void ResumeAllTroops() {
        foreach(var troop in instantiatedTroops) {
            if(troop.activeSelf) {
                troop.GetComponent<NPCPatroller>().Move();
            }
        }
    }

    private IEnumerator SpawnRegularly() {
        GameObject troop;
        int randomIndex;
        float routineInterval;

        if(latestPopedTroop!=null && !latestPopedTroop.activeSelf && !disappearedTroops.Contains(latestPopedTroop)) {
            disappearedTroops.Add(latestPopedTroop);
        }

        while(true) {
            while(disappearedTroops.Count == 0) {
                yield return null;
            }

            randomIndex = Random.Range(0, disappearedTroops.Count);
            troop = disappearedTroops[randomIndex];
            latestPopedTroop = troop;
            disappearedTroops.RemoveAt(randomIndex);

            Collider2D cd2d = troop.transform.Find("PlayerDetector").gameObject.GetComponent<Collider2D>();
            
            routineInterval = Random.Range(minRegularSpawnInterval, maxRegularSpawnInterval);
            yield return DelaySpawn(troop, cd2d, routineInterval);
        }
    }

    private IEnumerator DelaySpawn(GameObject troop, Collider2D cd2d, float delay) {
        yield return new WaitForSeconds(delay);

        Vector2 startPoint = new();
        Vector2 cd2dOffset = cd2d.offset;
        float deltaXWithCameraBound = Random.Range(minDeltaXWithCameraBound, maxDeltaXWithCameraBound);
        if(Random.Range(0, 2) == 0) {
            startPoint.x = playerRigidbody2D.position.x - deltaXWithCameraBound;
            cd2dOffset.x = 2;
        } else {
            startPoint.x = playerRigidbody2D.position.x + deltaXWithCameraBound;
            cd2dOffset.x = -2;
        }
        startPoint.y = troop.transform.position.y;

        troop.SetActive(true);

        troop.transform.position = (startPoint);
        cd2d.offset = cd2dOffset;
        Physics2D.SyncTransforms();

        troop.GetComponent<NPCPatroller>().Move();

        StartSpawningRegularly();
    }

    private void TroopDisappearingEventHandler(int id) {
        disappearedTroops.Add(instantiatedTroops[id]);
    }
}
