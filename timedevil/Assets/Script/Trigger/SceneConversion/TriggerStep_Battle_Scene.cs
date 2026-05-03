// Assets/Script/Trigger/SceneConversion/TriggerStep_Battle_Scene.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class TriggerStep_Battle_Scene : TriggerStepBase
{
    [Header("Battle Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

    [Header("Use SceneVisitEffectRunner (recommended)")]
    [SerializeField] private bool useSceneVisitEffectRunner = true;
    [SerializeField] private MonoBehaviour runnerOverride; // SceneVisitEffectRunner

    [Header("Return Point")]
    [Tooltip("복귀 위치. 비우면 ctx.player 또는 PlayerMainManager의 현재 위치를 사용")]
    [SerializeField] private Transform returnPointOverride;

    [Header("Return Policy")]
    [SerializeField] private float graceSeconds = 0.5f;
    [SerializeField] private bool requestCameraRebind = false;
    [SerializeField] private string worldVcamName = "CM vcam1";

    [Header("Return Camera Override (optional)")]
    [SerializeField] private bool useReturnCameraOverride = false;
    [SerializeField] private CameraModeId returnCameraModeOverride = CameraModeId.FollowFree;
    [SerializeField] private Transform returnFixedCameraAnchorOverride;
    [SerializeField] private Collider2D returnConfinerBoundsOverride;
    [SerializeField] private float returnOrthoSizeOverride = 0f;

    [Header("Return Camera Snapshot (optional)")]
    [SerializeField] private bool captureCameraSnapshot = true;

    [Header("Lock (optional)")]
    [SerializeField] private bool lockPlayerInput = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool _heldActionLock = false;
    private bool _sceneLoadedHooked = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        string targetScene = battleSceneName;
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning("[TriggerStep_Battle_Scene] battleSceneName이 비어있습니다.");
            yield break;
        }

        if (lockPlayerInput && GameManager.Instance)
        {
            if (!_heldActionLock)
            {
                GameManager.Instance.LockAction();
                _heldActionLock = true;
            }

            HookSceneLoadedUnlock();
        }

        SaveReturnContext(ctx);

        if (debugLog)
            Debug.Log($"[TriggerStep_Battle_Scene] Request Load '{targetScene}' mode={loadMode} useRunner={useSceneVisitEffectRunner}");

        if (useSceneVisitEffectRunner && loadMode == LoadSceneMode.Single)
        {
            var runner = ResolveRunner();
            if (runner != null)
            {
                runner.LoadSceneWithExitEffect(targetScene);
                yield break;
            }
        }

        SceneManager.LoadScene(targetScene, loadMode);
    }

    private void SaveReturnContext(TriggerContext ctx)
    {
        string curScene = SceneManager.GetActiveScene().name;

        Vector2 pos;
        if (returnPointOverride != null)
        {
            pos = returnPointOverride.position;
        }
        else if (ctx != null && ctx.player != null)
        {
            pos = ctx.player.position;
        }
        else
        {
            var player = Object.FindObjectOfType<PlayerMainManager>(true);
            pos = player ? (Vector2)player.transform.position : Vector2.zero;
        }

        bool restoreCam = false;
        CameraModeId camMode = CameraModeId.Fixed;
        float camOrtho = 0f;
        Vector2 camFixedPos = pos;
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

        // 배틀 복귀는 overlap suppression 비활성화 정책
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
            cameraBoundsName: camBoundsName
        );

        if (debugLog)
        {
            Debug.Log(
                $"[TriggerStep_Battle_Scene] Saved Return: scene='{curScene}', pos=({pos.x:F2},{pos.y:F2}), " +
                $"camRestore={restoreCam} camMode={camMode} camOrtho={camOrtho:F2} camBounds='{camBoundsName}'"
            );
        }
    }

    private SceneVisitEffectRunner ResolveRunner()
    {
        if (runnerOverride != null)
        {
            var r = runnerOverride as SceneVisitEffectRunner;
            if (r != null) return r;
        }

        return SceneVisitEffectRunner.Current;
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
}
