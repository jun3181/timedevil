using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
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
    private Transform enemy;
    private INPCMoveController npcMoveController;
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
        ResolvePlayer();

        enemy = GetComponent<Transform>();
        npcMoveController = GetComponent<INPCMoveController>();
    }

    public void Interact() {
        npcMoveController?.Idle();

        if(DialogueManager.instance!=null) {
            DialogueManager.instance.StartDialogue(dialogue);
            StartCoroutine(RunAfterDialogueFinish());
        } else {
            if(debuged) Debug.LogWarning($"{gameObject.name}에서 대사를 출력하려고 하였으나 DialogueManager.instance가 null입니다.");
            StartBattle(null);
        }
    }

    private IEnumerator RunAfterDialogueFinish() {
        while(true) {
            if(!DialogueManager.instance.isDialogueActive) break;
            yield return null;
        }

        StartBattle(enemy);
        yield break;
    }

    private void StartBattle(Transform enemySnapshotTarget) {
        ResolvePlayer();
        SaveReturnCameraContext();
        BattleSceneLoader.Go(BATTLE_SCENE, enemySO.enemyId, player, enemySnapshotTarget);
    }

    private void ResolvePlayer() {
        if(player != null) return;

        PlayerMove pm = FindObjectOfType<PlayerMove>(true);
        if(pm != null) {
            player = pm.transform;
            return;
        }

        PlayerMainManager pmm = FindObjectOfType<PlayerMainManager>(true);
        if(pmm != null) {
            player = pmm.transform;
        }
    }

    private void SaveReturnCameraContext() {
        Vector2 returnPos = player != null ? (Vector2)player.position : (Vector2)transform.position;

        bool restoreCam = false;
        CameraModeId camMode = CameraModeId.Fixed;
        float camOrtho = 0f;
        Vector2 camFixed = returnPos;
        string camBounds = null;

        var cm = CameraManager.Instance != null ? CameraManager.Instance : FindObjectOfType<CameraManager>(true);
        if(cm != null && cm.TryGetSnapshot(out camMode, out camOrtho, out Vector3 fixedPos, out string boundsName)) {
            restoreCam = true;
            camFixed = new Vector2(fixedPos.x, fixedPos.y);
            camBounds = string.IsNullOrWhiteSpace(boundsName) ? null : boundsName;
        }

        PlayerReturnContext.SetReturnFromTrigger(
            returnSceneName: SceneManager.GetActiveScene().name,
            returnPosition: returnPos,
            graceSeconds: 0f,
            requestCameraRebind: false,
            targetVcamName: null,
            useOverlapSuppression: false,
            overlapRadius: 0f,
            overlapSeconds: 0f,
            restoreCameraState: restoreCam,
            cameraMode: camMode,
            cameraOrthoSize: camOrtho,
            cameraFixedPos: camFixed,
            cameraBoundsName: camBounds
        );
    }
}
