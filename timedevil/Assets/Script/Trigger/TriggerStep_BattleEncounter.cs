using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class TriggerStep_BattleEncounter : TriggerStepBase
{
    [Header("Battle")]
    [SerializeField] private string battleSceneName = "battle";
    [SerializeField] private EnemyDatabaseSO enemyDatabase;
    [SerializeField] private EnemySO enemySO;
    [SerializeField] private bool requireEnemyInDatabase = true;

    [Header("Dialogue")]
    [SerializeField] private Dialogue beforeBattleDialogue;
    [SerializeField] private bool waitDialogueFinish = true;

    [Header("Actor")]
    [SerializeField] private Transform enemySnapshotTarget;
    [SerializeField] private bool useSelfAsEnemySnapshotWhenEmpty = true;
    [SerializeField] private bool stopNpcMovementBeforeBattle = true;

    [Header("Return")]
    [SerializeField] private Transform returnPointOverride;
    [SerializeField] private float graceSeconds = 0.5f;
    [SerializeField] private bool requestCameraRebind = false;
    [SerializeField] private string worldVcamName = "CM vcam1";

    [Header("Return Camera")]
    [SerializeField] private bool useReturnCameraOverride = false;
    [SerializeField] private CameraModeId returnCameraModeOverride = CameraModeId.FollowFree;
    [SerializeField] private Transform returnFixedCameraAnchorOverride;
    [SerializeField] private Collider2D returnConfinerBoundsOverride;
    [SerializeField] private float returnOrthoSizeOverride = 0f;
    [SerializeField] private bool captureCameraSnapshot = true;

    [Header("Victory Route On Return")]
    [FormerlySerializedAs("victoryRouterOverride")]
    [SerializeField] private TriggerRouter victoryRouter;
    [SerializeField] private string victoryRouteKey = "";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override bool AllowPlayerInputWhileExecuting => false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!TryResolveEnemyId(out string enemyId))
            yield break;

        Transform player = ResolvePlayer(ctx);
        if (player == null)
        {
            Debug.LogWarning("[TriggerStep_BattleEncounter] Player transform was not found.", this);
            yield break;
        }

        if (stopNpcMovementBeforeBattle)
        {
            var npcMoveController = GetComponent<INPCMoveController>();
            npcMoveController?.Idle();
        }

        if (HasDialogueContent(beforeBattleDialogue))
            yield return PlayBeforeBattleDialogue();

        SaveReturnContext(player);
        ArmVictoryRouteIfNeeded(enemyId);

        Transform snapshotTarget = ResolveEnemySnapshotTarget();

        if (debugLog)
        {
            Debug.Log(
                $"[TriggerStep_BattleEncounter] enter battle scene='{battleSceneName}' enemyId='{enemyId}' " +
                $"player='{player.name}' snapshot='{(snapshotTarget ? snapshotTarget.name : "null")}'",
                this);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.LockAction();

        BattleSceneLoader.Go(battleSceneName, enemyId, player, snapshotTarget);
    }

    private IEnumerator PlayBeforeBattleDialogue()
    {
        DialogueManager dm = DialogueManager.instance;
        if (dm == null)
        {
            if (debugLog)
                Debug.LogWarning("[TriggerStep_BattleEncounter] DialogueManager.instance is missing. Skipping dialogue.", this);

            yield break;
        }

        dm.StartDialogue(beforeBattleDialogue);

        if (!waitDialogueFinish)
            yield break;

        while (dm != null && dm.isDialogueActive)
            yield return null;
    }

    private bool TryResolveEnemyId(out string enemyId)
    {
        enemyId = null;

        if (enemySO == null)
        {
            Debug.LogWarning("[TriggerStep_BattleEncounter] enemySO is missing.", this);
            return false;
        }

        enemyId = string.IsNullOrWhiteSpace(enemySO.enemyId) ? null : enemySO.enemyId.Trim();
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            Debug.LogWarning("[TriggerStep_BattleEncounter] enemySO.enemyId is empty.", this);
            return false;
        }

        if (!requireEnemyInDatabase)
            return true;

        if (enemyDatabase == null)
        {
            Debug.LogWarning("[TriggerStep_BattleEncounter] enemyDatabase is missing.", this);
            return false;
        }

        if (enemyDatabase.GetById(enemyId) == null)
        {
            Debug.LogWarning($"[TriggerStep_BattleEncounter] Enemy id '{enemyId}' was not found in EnemyDatabaseSO.", this);
            return false;
        }

        return true;
    }

    private Transform ResolvePlayer(TriggerContext ctx)
    {
        if (ctx != null && ctx.player != null)
            return ctx.player;

        if (ctx != null && ctx.instigator != null)
        {
            PlayerMove ctxPlayerMove = ctx.instigator.GetComponentInParent<PlayerMove>();
            if (ctxPlayerMove != null)
                return ctxPlayerMove.transform;

            PlayerMainManager ctxPlayerManager = ctx.instigator.GetComponentInParent<PlayerMainManager>();
            if (ctxPlayerManager != null)
                return ctxPlayerManager.transform;
        }

        PlayerMove pm = FindObjectOfType<PlayerMove>(true);
        if (pm != null)
            return pm.transform;

        PlayerMainManager pmm = FindObjectOfType<PlayerMainManager>(true);
        return pmm != null ? pmm.transform : null;
    }

    private Transform ResolveEnemySnapshotTarget()
    {
        if (enemySnapshotTarget != null)
            return enemySnapshotTarget;

        return useSelfAsEnemySnapshotWhenEmpty ? transform : null;
    }

    private void SaveReturnContext(Transform player)
    {
        Vector2 returnPos = returnPointOverride != null
            ? (Vector2)returnPointOverride.position
            : (player != null ? (Vector2)player.position : (Vector2)transform.position);

        bool restoreCam = false;
        CameraModeId camMode = CameraModeId.Fixed;
        float camOrtho = 0f;
        Vector2 camFixedPos = returnPos;
        string camBoundsName = null;

        if (useReturnCameraOverride)
        {
            restoreCam = true;
            camMode = returnCameraModeOverride;
            camOrtho = returnOrthoSizeOverride;
            camFixedPos = returnFixedCameraAnchorOverride != null
                ? (Vector2)returnFixedCameraAnchorOverride.position
                : returnPos;
            camBoundsName = returnConfinerBoundsOverride != null ? returnConfinerBoundsOverride.name : null;
        }
        else if (captureCameraSnapshot)
        {
            CameraManager cm = CameraManager.Instance != null
                ? CameraManager.Instance
                : FindObjectOfType<CameraManager>(true);

            if (cm != null && cm.TryGetSnapshot(out camMode, out camOrtho, out Vector3 fixedPos3, out string boundsName))
            {
                restoreCam = true;
                camFixedPos = new Vector2(fixedPos3.x, fixedPos3.y);
                camBoundsName = string.IsNullOrWhiteSpace(boundsName) ? null : boundsName;
            }
        }

        PlayerReturnContext.SetReturnFromTrigger(
            returnSceneName: SceneManager.GetActiveScene().name,
            returnPosition: returnPos,
            graceSeconds: graceSeconds,
            requestCameraRebind: requestCameraRebind,
            targetVcamName: worldVcamName,
            useOverlapSuppression: false,
            overlapRadius: 0f,
            overlapSeconds: 0f,
            restoreCameraState: restoreCam,
            cameraMode: camMode,
            cameraOrthoSize: camOrtho,
            cameraFixedPos: camFixedPos,
            cameraBoundsName: camBoundsName
        );
    }

    private void ArmVictoryRouteIfNeeded(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(victoryRouteKey))
        {
            BattleVictoryReturnContext.ClearArmed();
            return;
        }

        if (victoryRouter == null)
        {
            if (debugLog)
                Debug.LogWarning("[TriggerStep_BattleEncounter] Victory Router is missing.", this);

            BattleVictoryReturnContext.ClearArmed();
            return;
        }

        string routerPath = BattleVictoryReturnContext.GetTransformPath(victoryRouter.transform);

        BattleVictoryReturnContext.Arm(
            targetSceneName: SceneManager.GetActiveScene().name,
            routeKey: victoryRouteKey,
            routerTransformPath: routerPath,
            enemyId: enemyId,
            sourceObjectName: name
        );
    }

    private static bool HasDialogueContent(Dialogue dialogue)
    {
        if (dialogue == null)
            return false;

        if (dialogue.lines != null)
        {
            for (int i = 0; i < dialogue.lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(dialogue.lines[i].text))
                    return true;
            }
        }

        if (dialogue.sentences != null)
        {
            for (int i = 0; i < dialogue.sentences.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(dialogue.sentences[i]))
                    return true;
            }
        }

        return false;
    }
}
