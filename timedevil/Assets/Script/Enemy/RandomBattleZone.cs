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
        [Tooltip("트리거 발동시 해당 적과 싸우게 될 가능성(가중치로 표현)")]
        public float weight;
    }

    [Header("조우 확률")]
    [Tooltip("1초를 기준으로 영역내에 존재해 있을 때 배틀에 참가될 확률(0~100%)")]
    [SerializeField] float probability;

    [Header("적 DB")]
    [SerializeField] EnemyDatabaseSO db;

    [Header("적 리스트")]
    [SerializeField] List<EnemyInfo> enemyInfos = new();

    [Header("디버그 용")]
    [SerializeField] bool debuged = true;

    private const string BATTLE_SCENE = "battle";
    private const float WAIT_SECS = 0.5f;
    private readonly WaitForSeconds WAIT_INTERVAL = new(WAIT_SECS);

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
            if(db.GetById(enemyInfos[i].enemySO.enemyId) == null || enemyInfos[i].weight == 0) {
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
        float adjustedProb = (1 - Mathf.Pow(1 - probability/100, WAIT_SECS)) * 100;
        float matchFactor, enemyFactor, scale = 0;
        if(debuged) Debug.Log($"{gameObject.name}의 {WAIT_SECS}초당 적 조우 확률은 {adjustedProb}");
        while(true) {
            yield return WAIT_INTERVAL;

            matchFactor = Random.Range(0f,100f);
            if(matchFactor<=adjustedProb) {
                enemyFactor = Random.Range(0f, totalWeight);
                for(int i=0; i<enemyInfos.Count; i++) {
                    scale += enemyInfos[i].weight;
                    if(scale>=enemyFactor) {
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
