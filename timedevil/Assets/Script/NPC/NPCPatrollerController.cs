using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.U2D.IK;

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
    [SerializeField] private float maxSpawnDeltaXWithCameraBound = 8f;
    [SerializeField] private float minSpawnDeltaXWithCameraBound = 4f;

    [Header("순찰병 디스폰시 카메라 경계와 X좌표 차")]
    [SerializeField] private float dispawnDeltaXWithCameraBound = 5f;

    [Header("순찰병 스폰 Y좌표")]
    [SerializeField] private float spawnYPosition = 1.5f;

    [Header("순찰병 즉시 스폰 후 반복 스폰")]
    public bool spawningRegularlyAfterInstantSpawn = true;

    private GameObject troop;
    private Rigidbody2D troopRigidbody2D;
    private NPCPatrollerPlayerDetector troopPlayerDetector;
    private bool isTroopAppeared = false;

    private IEnumerator regularlySpawningCoroutine;

    void Awake() {
        if(instance!=null || troopPrefab==null) {
            Destroy(gameObject);
        }

        instance = this;

        player = GameObject.FindWithTag("Player");
        playerRigidbody2D = player.GetComponent<Rigidbody2D>();
        playerCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

        NPCPatroller.OnDisappearing += TroopDisappearingEventHandler;

        troop = Instantiate(troopPrefab);
        troopRigidbody2D = troop.GetComponent<Rigidbody2D>();
        troopPlayerDetector = troop.transform.Find("PlayerDetector").gameObject.GetComponent<NPCPatrollerPlayerDetector>();

        troop.transform.position = new(0, spawnYPosition);
        troop.SetActive(false);

        BaseHideout.OnStealthingEnter += OnStealthingEnterEventHandler;
        BaseHideout.OnStealthingExit += OnStealthingExitEventHandler;
    }

    void OnDestroy() {
        BaseHideout.OnStealthingEnter -= OnStealthingEnterEventHandler;
        BaseHideout.OnStealthingExit -= OnStealthingExitEventHandler;
    }

    public void StartSpawningRegularly() {
        if(regularlySpawningCoroutine != null) return;

        regularlySpawningCoroutine = SpawnRegularly();
        StartCoroutine(regularlySpawningCoroutine);
    }

    public void StopSpawningRegularly() {
        if(regularlySpawningCoroutine!=null) {
            StopCoroutine(regularlySpawningCoroutine);
            regularlySpawningCoroutine = null;
        }
    }

    public bool StartSpawningInstantly() {
        if(isTroopAppeared) return false;

        SpawnTroop();
        if(spawningRegularlyAfterInstantSpawn)
            StartSpawningRegularly();

        return true;
    }

    public new void StopAllCoroutines() {
        base.StopAllCoroutines();
    }

    public void IdleAllTroops() {
        if(troop.activeSelf)
            troop.GetComponent<NPCPatroller>().Idle();
    }

    public void ResumeAllTroops() {
        if(troop.activeSelf)
            troop.GetComponent<NPCPatroller>().Move();
    }

    private IEnumerator SpawnRegularly() {
        float routineInterval;

        while(true) {
            if(isTroopAppeared) {
                yield return null;
                continue;
            }
            
            routineInterval = Random.Range(minRegularSpawnInterval, maxRegularSpawnInterval);
            yield return new WaitForSeconds(routineInterval);

            if(isTroopAppeared) {
                continue;
            }

            SpawnTroop();
        }
    }

    private void SpawnTroop() {
        isTroopAppeared = true;
        NPCPatroller.disappearanceXDistance = playerCamera.orthographicSize * playerCamera.aspect + dispawnDeltaXWithCameraBound;

        Vector2 startPoint = troop.transform.position;

        float deltaXWithCameraBound = Random.Range(minSpawnDeltaXWithCameraBound, maxSpawnDeltaXWithCameraBound) + playerCamera.orthographicSize * playerCamera.aspect;
        if(Random.Range(0, 2) == 0) {
            startPoint.x = playerRigidbody2D.position.x - deltaXWithCameraBound;
            troop.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else {
            startPoint.x = playerRigidbody2D.position.x + deltaXWithCameraBound;
            troop.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        troop.SetActive(true);

        troop.transform.position = (startPoint);
        Physics2D.SyncTransforms();

        troop.GetComponent<NPCPatroller>().Move();
    }

    private void TroopDisappearingEventHandler(int id) {
        isTroopAppeared = false;
    }

    private void OnStealthingEnterEventHandler() {
        if(WantedPoster.DetachingCounter == 0 || WantedPoster.DetachingCounter == WantedPoster.InstanceCounter)
            return;
        else
            StopSpawningRegularly();
    }

    private void OnStealthingExitEventHandler() {
        if(WantedPoster.DetachingCounter == 0 || WantedPoster.DetachingCounter == WantedPoster.InstanceCounter)
            return;
        else
            StartSpawningRegularly();
    }
}
