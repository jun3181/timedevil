using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RandomBattleZone : MonoBehaviour
{
    [System.Serializable]
    public struct EnemyInfo {
        public EnemySO enemySO;
        [Tooltip("트리거 발동시 해당 적과 싸우게 될 가중치")]
        public uint weight;
    }

    [Header("조우 확률")]
    [Tooltip("1초를 기준으로 영역내에 존재해 있을 때 배틀에 참가될 확률(0~100%)")]
    [SerializeField] byte possibility;

    [Header("조우 가능 횟수")]
    [Tooltip("참일 경우 오직 한번만 작동함")]
    [SerializeField] bool onlyOnce = false;

    [Header("적 DB")]
    [SerializeField] EnemyDatabaseSO db;

    [Header("적 리스트")]
    [SerializeField] List<EnemyInfo> enemyInfos = new();

    [Header("디버그 용")]
    [SerializeField] bool debuged;

    private bool triggered = false;
    private uint totalWeight = 0;

    void Awake() {
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true;

        if(possibility <= 0) {
            if(debuged) Debug.LogWarning($"{gameObject.name} 영역의 적 조우 확률이 0이하입니다.");
            gameObject.SetActive(false);
        } else if(possibility > 100) {
            if(debuged) Debug.LogWarning($"{gameObject.name} 영역의 적 조우 확률이 100초과 입니다.");
            possibility = 100;
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
}
