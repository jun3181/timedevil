using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class NPCPatrollerController : MonoBehaviour
{
    public static NPCPatrollerController instance;

    private static GameObject player;
    private static Rigidbody2D playerRigidbody2D;

    [SerializeField]
    [Header("정찰병 프리팹")]
    private GameObject troopPrefab;

    [Header("주기적인 순찰병 스폰 최소/최대 시간 간격")]
    [Range(0f, 30f)]
    [SerializeField] private float minRegularSpawnInterval = 8f;
    [SerializeField] private float maxRegularSpawnInterval = 27f;

    [Header("직접적인 순찰병 스폰 최대 딜레이")]
    [Range(0f, 10f)]
    [SerializeField] private float maxInstantSpawnInterval = 3f;

    [Header("순찰병 스폰시 플래이어와 X좌표 차 범위")]
    [SerializeField] private float minDeltaXWithPlayer = 10f;
    [SerializeField] private float maxDeltaXWithPlayer = 15f;

    [Header("순찰병 스폰 Y좌표 범위")]
    [SerializeField] private float minSpawnYPosition = 1.5f;
    [SerializeField] private float maxSpawnYPosition = -3f;

    private GameObject[] instantiatedTroops = new GameObject[5];
    private Queue<GameObject> disappearedTroops = new();

    private float latestSpawnTime = 0f;

    private IEnumerator spawningCoroutine;

    void Awake() {
        if(instance!=null) {
            Destroy(gameObject);
        }

        instance = this;

        player = GameObject.FindWithTag("Player");
        playerRigidbody2D = player.GetComponent<Rigidbody2D>();

        NPCPatroller.OnDisappearing += TroopDisappearingEventHandler;

        for(int i=0; i<instantiatedTroops.Length; i++) {
            instantiatedTroops[i] = Instantiate(troopPrefab);
            disappearedTroops.Enqueue(instantiatedTroops[i]);
        }
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

    public 

    private IEnumerator SpawnRegularly() {
        Vector2 startPoint = new();
        float routineInterval, deltaXWithPlayer;
        GameObject troop;

        while(true) {
            while(disappearedTroops.Count == 0) {
                yield return null;
            }

            troop = disappearedTroops.Dequeue();
            routineInterval = Random.Range(minRegularSpawnInterval, maxRegularSpawnInterval);

            yield return new WaitForSeconds(routineInterval);

            deltaXWithPlayer = Random.Range(minDeltaXWithPlayer, maxDeltaXWithPlayer);
            if(Random.Range(0,2)==0) {
                startPoint.x = playerRigidbody2D.position.x - deltaXWithPlayer;
            } else {
                startPoint.x = playerRigidbody2D.position.x + deltaXWithPlayer;
            }
            startPoint.y = Random.Range(minSpawnYPosition, maxSpawnYPosition);

            troop.SetActive(true);

            troop.GetComponent<Rigidbody2D>().MovePosition(startPoint);
            troop.GetComponent<NPCPatroller>().Move();
            latestSpawnTime = Time.time;
        }
    }

    private void TroopDisappearingEventHandler(int id) {
        disappearedTroops.Enqueue(instantiatedTroops[id]);
    }
}
