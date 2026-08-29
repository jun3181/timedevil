// Assets/Script/Trigger/Steps/TriggerStep_Scene.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class TriggerStep_Scene : TriggerStepBase
{
    [Header("Target Scene")]
    [SerializeField] private string sceneName = "Myroom";
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

    [Header("Use SceneVisitEffectRunner (recommended)")]
    [SerializeField] private bool useSceneVisitEffectRunner = true;
    [SerializeField] private MonoBehaviour runnerOverride; // SceneVisitEffectRunner 넣어도 됨

    [Header("Lock (optional)")]
    [SerializeField] private bool lockPlayerInput = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    // =========================
    //  Sleep Load (Bed -> Dream)
    // =========================
    [Header("Sleep Load (Bed -> Dream)")]
    [Tooltip("켜면 씬 전환 직전에 SleepLoadContext.MarkPending()를 호출합니다. (ProgressLoadApplier가 소비)")]
    [SerializeField] private bool markAsSleepLoad = false;

    [Tooltip("켜면 progress.json(lastSceneName)에서 씬 이름을 읽어서 그 씬으로 이동합니다.")]
    [SerializeField] private bool loadSceneFromProgress = false;

    [Tooltip("켜면 progress.json에 lastSceneName이 있을 때만 저장된 씬/위치로 이동하고, 없으면 Target Scene을 사용합니다.")]
    [SerializeField] private bool loadSceneFromProgressIfAvailable = false;

    [Tooltip("progress.json에 lastSceneName이 비어있을 때 갈 폴백 씬")]
    [SerializeField] private string fallbackDreamSceneName = "Move_Tutorial";

    [Header("Cutscene Auto Start (optional)")]
    [Tooltip("켜면 다음 씬의 CutsceneRouter가 Start 시 이 Key를 우선 사용합니다.")]
    [SerializeField] private bool overrideCutsceneStartKey = false;

    [Tooltip("overrideCutsceneStartKey가 켜져 있을 때 다음 씬 CutsceneRouter에 전달할 Start Key")]
    [SerializeField] private string cutsceneStartKey = "CutScene1";

    [Header("Scene Start Camera (optional)")]
    [Tooltip("켜면 다음 씬 SceneCameraBootstrap의 시작 Ortho Size를 덮어씁니다. 0이면 CameraManager 기본값을 사용합니다.")]
    [SerializeField] private bool overrideSceneStartOrthoSize = false;

    [Tooltip("다음 씬 시작 카메라 Ortho Size. 0이면 텔레포트 afterOrthoSize=0처럼 기본값을 사용합니다.")]
    [SerializeField] private float sceneStartOrthoSize = 0f;

    [Header("Target Scene Camera (optional)")]
    [Tooltip("켜면 다음 씬 로드 후 카메라 모드/컨파이너를 이 Step 값으로 적용합니다.")]
    [SerializeField] private bool overrideTargetSceneCamera = false;

    [Tooltip("다음 씬에서 적용할 CameraManager 모드")]
    [SerializeField] private CameraModeId targetSceneCameraMode = CameraModeId.FollowFree;

    [Tooltip("다음 씬 카메라 Ortho Size. 0이면 CameraManager 기본값을 사용합니다.")]
    [SerializeField] private float targetSceneCameraOrthoSize = 0f;

    [Tooltip("Fixed/Cutscene 모드일 때 사용할 월드 좌표")]
    [SerializeField] private Vector3 targetSceneFixedCameraPosition = Vector3.zero;

    [Tooltip("FollowConfined 모드일 때 다음 씬에서 찾을 Collider2D 오브젝트 이름")]
    [SerializeField] private string targetSceneConfinerBoundsName = "";

    [Tooltip("다음 씬에서 우선 재탐색할 Cinemachine Virtual Camera 이름. 비우면 자동 탐색")]
    [SerializeField] private string targetScenePreferredVcamName = "";

    [Header("Target Scene Spawn (optional)")]
    [Tooltip("켜면 다음 씬의 SceneEntryProfile/SceneEntrySpawnPoint 중 같은 Key를 가진 지점으로 플레이어를 보냅니다.")]
    [SerializeField] private bool overrideTargetSceneSpawn = false;

    [Tooltip("다음 씬에서 찾을 SceneEntryProfile/SceneEntrySpawnPoint key")]
    [SerializeField] private string targetSceneSpawnKey = "";

    // =========================
    // Battle return context (existing)
    // =========================
    [Header("Return (Battle enter only)")]
    [Tooltip("켜면 '현재 씬으로 복귀' 정보를 저장하고 다음 씬으로 넘어갑니다.")]
    [SerializeField] private bool saveReturnContext = false;

    [Tooltip("켜면 복귀 위치/카메라를 좌표 스냅샷 대신 복귀 씬의 SceneEntryProfile key로 해석합니다.")]
    [SerializeField] private bool useReturnSceneEntry = false;

    [Tooltip("복귀 씬 SceneEntryProfile에서 찾을 entry key")]
    [SerializeField] private string returnSceneEntryKey = "";

    [Tooltip("복귀 위치. useReturnSceneEntry가 꺼져 있으면 이 좌표를 사용합니다. 비우면 PlayerMainManager 현재 위치 저장.")]
    [SerializeField] private Transform returnPointOverride;

    [Tooltip("복귀 후 재진입 방지(옵션)")]
    [SerializeField] private float graceSeconds = 0.5f;

    [Tooltip("복귀 후 카메라 재바인딩 요청(옵션)")]
    [SerializeField] private bool requestCameraRebind = false;

    [SerializeField] private string worldVcamName = "CM vcam1";

    [Header("B Suppression (Overlap)")]
    [SerializeField] private bool useOverlapSuppression = true;
    [SerializeField] private float suppressOverlapRadius = 0.6f;
    [SerializeField] private float suppressOverlapSeconds = 1.5f;

    [Header("Return Camera Override (강제 저장)")]
    [Tooltip("체크하면 '복귀 카메라 상태'를 강제로 아래 값으로 저장합니다.")]
    [SerializeField] private bool useReturnCameraOverride = false;

    [SerializeField] private CameraModeId returnCameraModeOverride = CameraModeId.FollowFree;

    [Tooltip("Fixed/Cutscene일 때 복귀 카메라 고정 위치(Anchor). 비우면 복귀 위치(ReturnPosition)로 저장")]
    [SerializeField] private Transform returnFixedCameraAnchorOverride;

    [Tooltip("FollowConfined일 때 사용할 bounds. (복귀 시 이름으로 재탐색)")]
    [SerializeField] private Collider2D returnConfinerBoundsOverride;

    [Tooltip("0이면 복귀 시 CameraManager 기본 Ortho 유지")]
    [SerializeField] private float returnOrthoSizeOverride = 0f;

    [Header("Return Camera Snapshot (옵션)")]
    [Tooltip("Override를 끄면, 배틀 진입 직전 '현재 카메라 상태'를 스냅샷으로 저장합니다(가능한 경우).")]
    [SerializeField] private bool captureCameraSnapshot = true;

    private bool _heldActionLock = false;
    private bool _sceneLoadedHooked = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        // -------------------------
        // 1) 이번에 로드할 씬 이름 결정
        // -------------------------
        string targetScene = sceneName;
        bool useProgressLoad = loadSceneFromProgress;

        if (loadSceneFromProgress || loadSceneFromProgressIfAvailable)
        {
            // progress.json에서 lastSceneName 읽기
            var prog = ProgressSaveStore.Load();
            bool hasProgressTarget = !string.IsNullOrWhiteSpace(prog.lastSceneName);

            if (hasProgressTarget)
            {
                targetScene = prog.lastSceneName;
                useProgressLoad = true;
            }
            else if (loadSceneFromProgress)
            {
                targetScene = fallbackDreamSceneName;
            }
        }

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning("[TriggerStep_Scene] targetScene이 비어있습니다.");
            yield break;
        }

        if (useProgressLoad)
        {
            CutsceneStartContext.Clear();
        }
        else if (overrideCutsceneStartKey)
        {
            if (string.IsNullOrWhiteSpace(cutsceneStartKey))
            {
                CutsceneStartContext.Clear();
                Debug.LogWarning("[TriggerStep_Scene] overrideCutsceneStartKey가 켜져 있지만 cutsceneStartKey가 비어 있습니다.");
            }
            else
            {
                CutsceneStartContext.SetNext(targetScene, cutsceneStartKey);

                if (debugLog)
                    Debug.Log($"[TriggerStep_Scene] Queue Cutscene Start Key '{cutsceneStartKey}' for scene '{targetScene}'");
            }
        }
        else
        {
            CutsceneStartContext.Clear();
        }

        if (useProgressLoad)
        {
            SceneStartCameraContext.Clear();
        }
        else if (overrideSceneStartOrthoSize)
        {
            SceneStartCameraContext.SetNext(targetScene, sceneStartOrthoSize);

            if (debugLog)
            {
                string sizeText = sceneStartOrthoSize > 0f ? sceneStartOrthoSize.ToString("F2") : "default";
                Debug.Log($"[TriggerStep_Scene] Queue Scene Start Ortho '{sizeText}' for scene '{targetScene}'");
            }
        }
        else
        {
            SceneStartCameraContext.Clear();
        }

        if (useProgressLoad)
        {
            SceneEntrySpawnContext.Clear();
        }
        else if (overrideTargetSceneSpawn)
        {
            if (string.IsNullOrWhiteSpace(targetSceneSpawnKey))
            {
                SceneEntrySpawnContext.Clear();
                Debug.LogWarning("[TriggerStep_Scene] overrideTargetSceneSpawn is on, but targetSceneSpawnKey is empty.");
            }
            else
            {
                SceneEntrySpawnContext.SetNext(targetScene, targetSceneSpawnKey);

                if (debugLog)
                    Debug.Log($"[TriggerStep_Scene] Queue target spawn '{targetSceneSpawnKey}' for scene '{targetScene}'");
            }
        }
        else
        {
            SceneEntrySpawnContext.Clear();
        }

        // -------------------------
        // 2) SleepLoad 플래그(원샷) 세팅
        // -------------------------
        if (markAsSleepLoad || useProgressLoad)
        {
            SleepLoadContext.MarkPending();
            if (debugLog) Debug.Log("[TriggerStep_Scene] SleepLoadContext.MarkPending()");
        }

        // -------------------------
        // 3) 입력 잠금
        // -------------------------
        if (lockPlayerInput && GameManager.Instance)
        {
            if (!_heldActionLock)
            {
                GameManager.Instance.LockAction();
                _heldActionLock = true;
            }

            HookSceneLoadedUnlock();
        }

        if (debugLog)
            Debug.Log($"[TriggerStep_Scene] Request Load '{targetScene}' mode={loadMode} useRunner={useSceneVisitEffectRunner}");

        SceneCameraRequest targetSceneCameraRequest = BuildTargetSceneCameraRequest();

        // -------------------------
        // 4) (기존) 배틀 진입이라면 복귀 정보 저장
        // -------------------------
        if (!useProgressLoad && saveReturnContext)
        {
            string curScene = SceneManager.GetActiveScene().name;
            string returnEntryKey = null;

            if (useReturnSceneEntry)
            {
                returnEntryKey = string.IsNullOrWhiteSpace(returnSceneEntryKey)
                    ? null
                    : returnSceneEntryKey.Trim();

                if (string.IsNullOrWhiteSpace(returnEntryKey))
                    Debug.LogWarning("[TriggerStep_Scene] useReturnSceneEntry is on, but returnSceneEntryKey is empty.");
            }

            Vector2 pos;
            if (returnPointOverride != null) pos = returnPointOverride.position;
            else
            {
                var player = Object.FindObjectOfType<PlayerMainManager>(true);
                pos = player ? (Vector2)player.transform.position : Vector2.zero;
            }

            bool restoreCam = false;
            CameraModeId camMode = CameraModeId.Fixed;
            float camOrtho = 0f;
            Vector2 camFixedPos = Vector2.zero;
            string camBoundsName = null;

            if (useReturnCameraOverride)
            {
                restoreCam = true;
                camMode = returnCameraModeOverride;
                camOrtho = returnOrthoSizeOverride;

                Vector2 fixedPos = (returnFixedCameraAnchorOverride != null)
                    ? (Vector2)returnFixedCameraAnchorOverride.position
                    : pos;
                camFixedPos = fixedPos;

                camBoundsName = (returnConfinerBoundsOverride != null) ? returnConfinerBoundsOverride.name : null;
            }
            else if (captureCameraSnapshot && CameraManager.Instance != null)
            {
                if (CameraManager.Instance.TryGetSnapshot(out camMode, out camOrtho, out Vector3 fixedPos3, out string boundsName))
                {
                    restoreCam = true;
                    camFixedPos = new Vector2(fixedPos3.x, fixedPos3.y);
                    camBoundsName = string.IsNullOrWhiteSpace(boundsName) ? null : boundsName;
                }
            }

            // (A) 정책: 배틀 복귀 시 overlap suppression OFF
            bool overlapSupp = false;
            float overlapRadius = 0f;
            float overlapSec = 0f;

            PlayerReturnContext.SetReturnFromTrigger(
                returnSceneName: curScene,
                returnPosition: pos,
                graceSeconds: graceSeconds,
                requestCameraRebind: requestCameraRebind,
                targetVcamName: worldVcamName,
                useOverlapSuppression: overlapSupp,
                overlapRadius: overlapRadius,
                overlapSeconds: overlapSec,
                restoreCameraState: restoreCam,
                cameraMode: camMode,
                cameraOrthoSize: camOrtho,
                cameraFixedPos: camFixedPos,
                cameraBoundsName: camBoundsName,
                returnEntryKey: returnEntryKey
            );

            if (debugLog)
            {
                Debug.Log(
                    $"[TriggerStep_Scene] Saved Return(A-Policy): scene='{curScene}', pos=({pos.x:F2},{pos.y:F2}) " +
                    $"entry='{returnEntryKey}' " +
                    $"overlapSupp=OFF " +
                    $"camRestore={restoreCam} camMode={camMode} camOrtho={camOrtho:F2} camBounds='{camBoundsName}' camFixed=({camFixedPos.x:F2},{camFixedPos.y:F2})"
                );
            }
        }

        // -------------------------
        // 5) 씬 로드
        // -------------------------
        if (useProgressLoad)
        {
            SceneTransitionService.LoadProgressSave(fallbackDreamSceneName, useSceneVisitEffectRunner);
            yield break;
        }

        if (!useProgressLoad && saveReturnContext)
        {
            SceneTransitionService.EnterBattle(targetScene, null, null, null, useSceneVisitEffectRunner);
            yield break;
        }

        if (overrideTargetSceneSpawn && !string.IsNullOrWhiteSpace(targetSceneSpawnKey))
        {
            if (targetSceneCameraRequest.hasCamera)
            {
                SceneTransitionService.LoadSpawn(
                    targetScene,
                    targetSceneSpawnKey,
                    targetSceneCameraRequest,
                    true,
                    useSceneVisitEffectRunner,
                    loadMode);
            }
            else
            {
                SceneTransitionService.LoadSpawn(targetScene, targetSceneSpawnKey, useSceneVisitEffectRunner, loadMode);
            }

            yield break;
        }

        if (targetSceneCameraRequest.hasCamera)
        {
            SceneTransitionService.LoadDefault(
                targetScene,
                targetSceneCameraRequest,
                true,
                useSceneVisitEffectRunner,
                loadMode);
            yield break;
        }

        SceneTransitionService.LoadDefault(targetScene, useSceneVisitEffectRunner, loadMode);
    }

    private SceneCameraRequest BuildTargetSceneCameraRequest()
    {
        if (!overrideTargetSceneCamera)
            return SceneCameraRequest.None;

        string boundsName = string.IsNullOrWhiteSpace(targetSceneConfinerBoundsName)
            ? null
            : targetSceneConfinerBoundsName.Trim();

        string preferredVcamName = string.IsNullOrWhiteSpace(targetScenePreferredVcamName)
            ? null
            : targetScenePreferredVcamName.Trim();

        SceneCameraRequest request = SceneCameraRequest.FromSnapshot(
            targetSceneCameraMode,
            targetSceneCameraOrthoSize,
            targetSceneFixedCameraPosition,
            boundsName,
            preferredVcamName
        );

        if (debugLog)
        {
            string sizeText = targetSceneCameraOrthoSize > 0f ? targetSceneCameraOrthoSize.ToString("F2") : "default";
            string boundsText = string.IsNullOrWhiteSpace(boundsName) ? "(none)" : boundsName;
            string vcamText = string.IsNullOrWhiteSpace(preferredVcamName) ? "(auto)" : preferredVcamName;
            Debug.Log($"[TriggerStep_Scene] Queue target camera mode={targetSceneCameraMode} ortho={sizeText} bounds='{boundsText}' vcam='{vcamText}'");
        }

        return request;
    }

    private void OnDisable()
    {
        UnhookSceneLoadedUnlock();
        ReleaseActionLockIfHeld();
    }

    private void HookSceneLoadedUnlock()
    {
        if (_sceneLoadedHooked) return;
        SceneManager.sceneLoaded += OnSceneLoadedReleaseLock;
        _sceneLoadedHooked = true;
    }

    private void UnhookSceneLoadedUnlock()
    {
        if (!_sceneLoadedHooked) return;
        SceneManager.sceneLoaded -= OnSceneLoadedReleaseLock;
        _sceneLoadedHooked = false;
    }

    private void OnSceneLoadedReleaseLock(Scene scene, LoadSceneMode mode)
    {
        ReleaseActionLockIfHeld();
        UnhookSceneLoadedUnlock();
    }

    private void ReleaseActionLockIfHeld()
    {
        if (!_heldActionLock || !GameManager.Instance) return;
        GameManager.Instance.UnlockAction();
        _heldActionLock = false;
    }

    private SceneVisitEffectRunner ResolveRunner()
    {
        if (runnerOverride != null)
        {
            if (runnerOverride is SceneVisitEffectRunner r) return r;

            if (debugLog)
                Debug.LogWarning($"[TriggerStep_Scene] runnerOverride 타입이 다릅니다: {runnerOverride.GetType().Name}");
        }

        if (SceneVisitEffectRunner.Current != null)
            return SceneVisitEffectRunner.Current;

        return Object.FindObjectOfType<SceneVisitEffectRunner>(true);
    }
}
