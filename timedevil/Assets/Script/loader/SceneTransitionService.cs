using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionService
{
    public static void LoadDefault(
        string targetScene,
        bool useFaderIfExists = true,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        SceneArrivalContext.Clear();
        ClearLegacyEntryOneShots();
        LoadScene(targetScene, useFaderIfExists, mode);
    }

    public static void LoadDefault(
        string targetScene,
        SceneCameraRequest cameraOverride,
        bool preserveCameraOverride = true,
        bool useFaderIfExists = true,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!cameraOverride.hasCamera)
        {
            LoadDefault(targetScene, useFaderIfExists, mode);
            return;
        }

        ClearLegacyEntryOneShots();

        var request = SceneArrivalRequest.Default(targetScene);
        ApplyCameraOverride(request, cameraOverride, preserveCameraOverride);
        SceneArrivalContext.SetNext(request);

        LoadScene(targetScene, useFaderIfExists, mode);
    }

    public static void LoadSpawn(
        string targetScene,
        string spawnKey,
        bool useFaderIfExists = true,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (string.IsNullOrWhiteSpace(spawnKey))
        {
            Debug.LogWarning("[SceneTransitionService] spawnKey is empty. Loading default scene entry.");
            LoadDefault(targetScene, useFaderIfExists, mode);
            return;
        }

        ClearLegacyEntryOneShots();
        SceneArrivalContext.SetNext(SceneArrivalRequest.SpawnKey(targetScene, spawnKey));
        LoadScene(targetScene, useFaderIfExists, mode);
    }

    public static void LoadSpawn(
        string targetScene,
        string spawnKey,
        SceneCameraRequest cameraOverride,
        bool preserveCameraOverride = true,
        bool useFaderIfExists = true,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!cameraOverride.hasCamera)
        {
            LoadSpawn(targetScene, spawnKey, useFaderIfExists, mode);
            return;
        }

        if (string.IsNullOrWhiteSpace(spawnKey))
        {
            Debug.LogWarning("[SceneTransitionService] spawnKey is empty. Loading default scene entry.");
            LoadDefault(targetScene, cameraOverride, preserveCameraOverride, useFaderIfExists, mode);
            return;
        }

        ClearLegacyEntryOneShots();

        var request = SceneArrivalRequest.SpawnKey(targetScene, spawnKey);
        ApplyCameraOverride(request, cameraOverride, preserveCameraOverride);
        SceneArrivalContext.SetNext(request);

        LoadScene(targetScene, useFaderIfExists, mode);
    }

    public static void LoadWorldPosition(
        string targetScene,
        Vector3 position,
        SceneCameraRequest camera,
        bool useFaderIfExists = true,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        var request = SceneArrivalRequest.WorldPosition(targetScene, position);
        request.camera = camera;
        ClearLegacyEntryOneShots();
        SceneArrivalContext.SetNext(request);
        LoadScene(targetScene, useFaderIfExists, mode);
    }

    public static void EnterMyroom(
        MyroomEntryPoint entryPoint,
        string myroomSceneName = "Myroom",
        bool useFaderIfExists = true)
    {
        ClearLegacyEntryOneShots();
        SceneArrivalContext.SetNext(SceneArrivalRequest.Myroom(myroomSceneName, entryPoint));
        LoadScene(myroomSceneName, useFaderIfExists, LoadSceneMode.Single);
    }

    public static void LoadProgressSave(
        string fallbackSceneName = "Move_Tutorial",
        bool useFaderIfExists = true)
    {
        ClearLegacyEntryOneShots(clearSleepLoadContext: false);
        SleepLoadContext.MarkPending();

        var data = ProgressSaveStore.Load();
        SaveSystem.LoadCheckpointRuntime();
        TriggerRuntimeSaveBridge.Restore(data.triggerRuntime);

        string targetScene = !string.IsNullOrWhiteSpace(data.lastSceneName)
            ? data.lastSceneName
            : fallbackSceneName;

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning("[SceneTransitionService] Progress target scene is empty.");
            return;
        }

        var request = SceneArrivalRequest.WorldPosition(targetScene, data.playerPos, SceneArrivalKind.ProgressLoad);
        request.camera = data.hasCamera
            ? SceneCameraRequest.FromSnapshot(data.cameraMode, data.cameraOrthoSize, data.cameraFixedPos, data.cameraBoundsName)
            : SceneCameraRequest.None;

        SceneArrivalContext.SetNext(request);
        LoadScene(targetScene, useFaderIfExists, LoadSceneMode.Single);
    }

    public static bool EnterBattle(
        string battleSceneName,
        string enemyId,
        Transform player,
        Transform enemy,
        bool useFaderIfExists = true)
    {
        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogWarning("[SceneTransitionService] battleSceneName is empty.");
            return false;
        }

        PrepareBattleDeckForEntry();

        SceneArrivalRequest returnRequest = BuildBattleReturnRequest(enemyId, player, enemy);
        if (returnRequest != null)
        {
            SceneArrivalContext.SetNext(returnRequest);
            PlayerReturnContext.ClearReturnCore();
        }

        ClearLegacyEntryOneShots();

        if (enemy != null && WorldNPCStateService.Instance != null)
            WorldNPCStateService.Instance.SaveSnapshot(enemy.gameObject);

        LoadScene(battleSceneName, useFaderIfExists, LoadSceneMode.Single);
        return true;
    }

    public static bool PrepareBattleDeckForEntry()
    {
        CardStateRuntime runtime = ResolveCardStateRuntimeForBattleEntry();
        if (runtime == null)
        {
            Debug.LogWarning("[SceneTransitionService] CardStateRuntime is missing. Battle will continue without deck preparation.");
            return true;
        }

        if (!runtime.TryPrepareDeckForBattle(out string failureReason))
            Debug.LogWarning($"[SceneTransitionService] {failureReason}");

        return true;
    }

    public static void ReturnFromBattle(float graceSeconds = 1.0f, bool useFaderIfExists = true)
    {
        if (!SceneArrivalContext.TryPeek(out SceneArrivalRequest request) ||
            request == null ||
            request.kind != SceneArrivalKind.BattleReturn)
        {
            request = BuildBattleReturnRequestFromLegacy();
            if (request != null)
                SceneArrivalContext.SetNext(request);
        }

        if (request == null || string.IsNullOrWhiteSpace(request.targetSceneName))
        {
            Debug.LogWarning("[SceneTransitionService] Battle return scene is empty.");
            return;
        }

        request.graceSeconds = Mathf.Max(request.graceSeconds, graceSeconds);
        PlayerReturnContext.GraceSecondsPending = request.graceSeconds;
        PlayerReturnContext.IsInGracePeriod = request.graceSeconds > 0f;

        BattleEncounterState.ClearPending();
        LoadScene(request.targetSceneName, useFaderIfExists, LoadSceneMode.Single);
    }

    public static SceneCameraRequest CaptureCurrentCamera(string preferredVcamName = null)
    {
        var cm = CameraManager.Instance ?? Object.FindObjectOfType<CameraManager>(true);
        if (cm != null && cm.TryGetSnapshot(out CameraModeId mode, out float ortho, out Vector3 fixedPos, out string boundsName))
            return SceneCameraRequest.FromSnapshot(mode, ortho, fixedPos, boundsName, preferredVcamName);

        return SceneCameraRequest.None;
    }

    private static SceneArrivalRequest BuildBattleReturnRequest(string enemyId, Transform player, Transform enemy)
    {
        SceneArrivalRequest legacy = BuildBattleReturnRequestFromLegacy();
        if (legacy != null)
        {
            AddEnemyInfo(legacy, enemyId, enemy);
            return legacy;
        }

        string returnScene = SceneManager.GetActiveScene().name;
        var request = SceneArrivalRequest.WorldPosition(
            returnScene,
            player != null ? player.position : Vector3.zero,
            SceneArrivalKind.BattleReturn
        );

        request.hasWorldPosition = player != null;
        request.camera = CaptureCurrentCamera();
        AddEnemyInfo(request, enemyId, enemy);
        return request;
    }

    private static SceneArrivalRequest BuildBattleReturnRequestFromLegacy()
    {
        if (string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName))
            return null;

        SceneArrivalRequest request;
        if (!string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnEntryKey))
        {
            request = SceneArrivalRequest.SpawnKey(
                PlayerReturnContext.ReturnSceneName,
                PlayerReturnContext.ReturnEntryKey,
                SceneArrivalKind.BattleReturn
            );
        }
        else
        {
            request = SceneArrivalRequest.WorldPosition(
                PlayerReturnContext.ReturnSceneName,
                PlayerReturnContext.ReturnPosition,
                SceneArrivalKind.BattleReturn
            );

            request.hasWorldPosition = PlayerReturnContext.HasReturnPosition;
        }

        request.graceSeconds = PlayerReturnContext.GraceSecondsPending;
        request.requestCameraRebind = PlayerReturnContext.CameraRebindRequested;
        request.targetVcamName = PlayerReturnContext.TargetVcamName;
        request.useOverlapSuppression = PlayerReturnContext.UseOverlapSuppression;
        request.overlapRadius = PlayerReturnContext.OverlapRadiusPending;
        request.overlapSeconds = PlayerReturnContext.OverlapSecondsPending;
        request.restoreEnemySnapshot = !string.IsNullOrWhiteSpace(PlayerReturnContext.MonsterInstanceId);
        request.enemyInstanceId = PlayerReturnContext.MonsterInstanceId;
        request.enemyNameInScene = PlayerReturnContext.MonsterNameInScene;

        request.camera = PlayerReturnContext.RestoreCameraStatePending
            ? SceneCameraRequest.FromSnapshot(
                PlayerReturnContext.ReturnCameraMode,
                PlayerReturnContext.ReturnCameraOrthoSize,
                PlayerReturnContext.ReturnCameraFixedPos,
                PlayerReturnContext.ReturnCameraBoundsName,
                PlayerReturnContext.TargetVcamName
            )
            : SceneCameraRequest.None;

        return request;
    }

    private static void AddEnemyInfo(SceneArrivalRequest request, string enemyId, Transform enemy)
    {
        if (request == null) return;

        if (enemy != null)
        {
            var instanceId = enemy.GetComponent<EnemyInstanceId>();
            request.enemyInstanceId = instanceId != null ? instanceId.Id : enemy.gameObject.name;
            request.enemyNameInScene = enemy.gameObject.name;
            request.restoreEnemySnapshot = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(enemyId) && string.IsNullOrWhiteSpace(request.enemyInstanceId))
            request.enemyInstanceId = enemyId;
    }

    private static void LoadScene(string sceneName, bool useFaderIfExists, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneTransitionService] sceneName is empty.");
            return;
        }

        if (mode != LoadSceneMode.Single)
        {
            SceneManager.LoadScene(sceneName, mode);
            return;
        }

        if (useFaderIfExists && SceneVisitEffectRunner.Current != null)
        {
            SceneVisitEffectRunner.Current.LoadSceneWithExitEffect(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private static CardStateRuntime ResolveCardStateRuntimeForBattleEntry()
    {
        if (CardStateRuntime.Instance != null)
            return CardStateRuntime.Instance;

        CardStateRuntime existing = Object.FindObjectOfType<CardStateRuntime>(true);
        if (existing != null)
            return existing;

        var runtimeObject = new GameObject("CardStateRuntime (Battle Entry Auto)");
        return runtimeObject.AddComponent<CardStateRuntime>();
    }

    private static void ApplyCameraOverride(
        SceneArrivalRequest request,
        SceneCameraRequest cameraOverride,
        bool preserveCameraOverride)
    {
        if (request == null || !cameraOverride.hasCamera)
            return;

        request.camera = cameraOverride;
        request.preserveCameraOverride = preserveCameraOverride;
    }

    private static void ClearLegacyEntryOneShots(bool clearSleepLoadContext = true)
    {
        SceneEntrySpawnContext.Clear();
        MyroomEntryContext.Clear();
        if (clearSleepLoadContext)
            SleepLoadContext.Consume();
    }
}
