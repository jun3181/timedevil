using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.SceneManagement;

using Random = UnityEngine.Random;


[RequireComponent(typeof(Collider2D))]
public class RandomBattleZone : MonoBehaviour
{
    [System.Serializable]
    public struct EnemyInfo {
        public EnemySO enemySO;
        [Tooltip("트리거 발동시 해당 적과 싸우게 될 가능성(상대적 확률)")]
        public float weight;
    }

    [Header("조우 확률(%)")]
    [Tooltip("플래이어가 '조우 확률 기준 시간' 동안 영역 내에 존재할 때 설정된 적들과 조우할 확률(0~100%)")]
    [SerializeField] float probability;

    [Header("조우 확률 기준 시간(초)")]
    [Tooltip("조우 확률의 기준이 되는 시간")]
    [SerializeField] float unitSecs = 5f;

    [Header("조우 결정 단위 시간(초)")]
    [Tooltip("조우 확률에 따라 적과의 조우를 결정하는 코루틴이 재개되는 시간(조우 확률에 영향을 미치지 않음)")]
    [SerializeField] float coroutineWaitSecs = 0.5f;

    [Header("적 DB")]
    [SerializeField] EnemyDatabaseSO db;

    [Header("적 리스트")]
    [SerializeField] List<EnemyInfo> enemyInfos = new();

    [Header("디버그 용")]
    [SerializeField] bool debuged = true;

    private const string BATTLE_SCENE = "battle";
    private WaitForSeconds WAIT_INTERVAL;

    private float adjustedProb = 0f;

    private float totalWeight = 0;

    private Collider2D collider2d;

    private IEnumerator matchRoutine = null;

    void Awake() {
        collider2d = GetComponent<Collider2D>();
        collider2d.enabled = true;

        if(probability <= 0) {
            if(debuged) Debug.LogWarning($"{gameObject.name} 영역의 적 조우 확률이 0이하입니다.");
            gameObject.SetActive(false);
        } else if(probability > 100) {
            if(debuged) Debug.LogWarning($"{gameObject.name} 영역의 적 조우 확률이 100초과 입니다.");
            probability = 100f;
        }

        for(int i = 0; i < enemyInfos.Count; i++) {
            if(enemyInfos[i].enemySO==null || db.GetById(enemyInfos[i].enemySO.enemyId) == null || enemyInfos[i].weight == 0) {
                enemyInfos.RemoveAt(i);
                i--;
                continue;
            }

            totalWeight += enemyInfos[i].weight;
        }

        if(enemyInfos.Count == 0) {
            if(debuged) Debug.LogWarning($"{gameObject.name} 영역에 유효한 적이 설정되지 않았습니다.");
            gameObject.SetActive(false);
            return;
        }

        if(unitSecs == 0) unitSecs = 5f;
        if(coroutineWaitSecs == 0) coroutineWaitSecs = 0.5f;

        if(coroutineWaitSecs>unitSecs) {
            if(debuged) Debug.LogWarning($"{gameObject.name}의 조우 결정 단위 시간이 조우 확률 기준 시간보다 큽니다.");
            coroutineWaitSecs = unitSecs;
        }

        WAIT_INTERVAL = new WaitForSeconds(coroutineWaitSecs);
        adjustedProb = (1 - Mathf.Pow(1 - probability / 100, 1 / (unitSecs / coroutineWaitSecs))) * 100;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player")) {
            Transform player = other.GetComponent<Transform>();
            matchRoutine = MatchBattle(player);
            StartCoroutine(matchRoutine);
        }
            
    }

    void OnTriggerExit2D(Collider2D other) {
        if(other.CompareTag("Player")) {
            StopCoroutine(matchRoutine);
            matchRoutine = null;
        }
    }

    private IEnumerator MatchBattle(Transform player) {
        float matchFactor, enemyFactor, scale = 0;
        
        if(debuged) Debug.Log($"{gameObject.name}의 {coroutineWaitSecs}초당 적 조우 확률은 {adjustedProb}%");
        
        while(true) {
            yield return WAIT_INTERVAL;

            matchFactor = Random.Range(0f,100f);
            if(matchFactor<=adjustedProb) {
                enemyFactor = Random.Range(0f, totalWeight);
                if(debuged) {
                    Debug.Log($"{gameObject.name}의 총 가중치는 {totalWeight}, 적 매칭 인자는 {enemyFactor}");
                }
               
                for(int i=0; i<enemyInfos.Count; i++) {
                    scale += enemyInfos[i].weight;
                    if(scale>=enemyFactor) {
                        if(debuged) Debug.Log($"{gameObject.name}에서 매칭된 적의 이름은 {enemyInfos[i].enemySO.enemyId}");
                        
                        BattleSceneLoader.Go(BATTLE_SCENE, enemyInfos[i].enemySO.enemyId, player, null);
                        matchRoutine = null;
                        yield break;
                    }
                }

                if(debuged) Debug.LogWarning($"{gameObject.name}에서 알 수 없는 이유로 적과 매칭되지 않음.");
            }
        }
    }
}
