using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBattleInteraction : MonoBehaviour, IInteractable
{
    private const string BATTLE_SCENE = "battle";

    [Header("적 DB")]
    public EnemyDatabaseSO db;

    [Header("상호작용 시 만나게 될 적")]
    public EnemySO enemySO;

    [Header("적과 만나기 전 대사")]
    public Dialogue dialogue;

    [Header("디버그 메시지 출력 여부")]
    public bool debuged = true;

    private Transform player;
    void Awake() {
        if(db==null) {
            if(debuged) Debug.LogWarning($"{gameObject.name}의 EnemyDatabaseSO가 설정되지 않았습니다.");
            return;
        }

        if(enemySO==null || db.GetById(enemySO.enemyId)==null) {
            if(debuged) Debug.LogWarning($"{gameObject.name}의 EnemySO가 유효하지 않습니다.");
            return;
        }
    }

    void Start() {
        PlayerMove pm = FindObjectOfType<PlayerMove>(true);
        if(pm != null) {
            player = pm.GetComponent<Transform>();
        }
    }

    public void Interact() {
        if(DialogueManager.instance!=null) {
            DialogueManager.instance.StartDialogue(dialogue);
        } else if(debuged) {
            Debug.LogWarning($"{gameObject.name}에서 대사를 출력하려고 하였으나 DialogueManager.instance가 null입니다.");
        }

        BattleSceneLoader.Go(BATTLE_SCENE, enemySO.enemyId, player, null);
    }
}
