using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCPatrollerController : MonoBehaviour
{
    public static NPCPatrollerController instance;

    [SerializeField]
    [Header("정찰병 프리팹")]
    private GameObject troopPrefab;

    [Header("병사 스폰 최소/최대 시간 간격")]
    [Range(0, 30)]
    [SerializeField] private float minSpawnInterval = 8f;
    [SerializeField] private float maxSpawnInterval = 27f;

    private GameObject[] instantiatedTroops = new GameObject[5];
    private Queue<GameObject> disappearedTroops = new();

    private float latestSpawnTime = 0f;

    private IEnumerator spawningCoroutine;

    void Awake() {
        if(instance!=null) {
            Destroy(gameObject);
        }

        instance = this;

        NPCPatroller.OnDisappearing += TroopDisappearingEventHandler;

        for(int i=0; i<instantiatedTroops.Length; i++) {
            instantiatedTroops[i] = Instantiate(troopPrefab);
            disappearedTroops.Enqueue(instantiatedTroops[i]);
        }
    }

    public void StartSpawningRepeatedly() {
        if(spawningCoroutine != null) return;

        spawningCoroutine = SpawnTroopRepeatedly();
        StartCoroutine(spawningCoroutine);
    }

    public void StopSpawningRepeatedly() {
        if(spawningCoroutine!=null) {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
    }

    private IEnumerator SpawnTroopRepeatedly() {
        float routineInterval;
        while(true) {
            routineInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            
        }
    }

    private void TroopDisappearingEventHandler(int id) {
        disappearedTroops.Enqueue(instantiatedTroops[id]);
    }
}
