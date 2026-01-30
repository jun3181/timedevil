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

    [Header("Return (Battle enter only)")]
    [Tooltip("켜면 '현재 씬으로 복귀' 정보를 저장하고 다음 씬으로 넘어갑니다.")]
    [SerializeField] private bool saveReturnContext = false;

    [Tooltip("복귀 위치. 비우면 PlayerMainManager 현재 위치 저장.")]
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

    [Header("Return Camera Override (★강제 저장)")]
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

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[TriggerStep_Scene] sceneName이 비어있습니다.");
            yield break;
        }

        if (lockPlayerInput && GameManager.Instance)
            GameManager.Instance.isAction = true;

        if (debugLog)
            Debug.Log($"[TriggerStep_Scene] Request Load '{sceneName}' mode={loadMode} useRunner={useSceneVisitEffectRunner}");

        // 배틀 진입이라면 "복귀 정보" 저장
        if (saveReturnContext)
        {
            string curScene = SceneManager.GetActiveScene().name;

            // 복귀 위치 저장
            Vector2 pos;
            if (returnPointOverride != null) pos = returnPointOverride.position;
            else
            {
                var player = Object.FindObjectOfType<PlayerMainManager>(true);
                pos = player ? (Vector2)player.transform.position : Vector2.zero;
            }

            // ===== 복귀 카메라 저장 (Override 우선) =====
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

                // Fixed/Cutscene: 앵커 없으면 "복귀 위치"를 고정 위치로 저장
                Vector2 fixedPos = (returnFixedCameraAnchorOverride != null)
                    ? (Vector2)returnFixedCameraAnchorOverride.position
                    : pos;
                camFixedPos = fixedPos;

                // FollowConfined: bounds는 이름 저장(복귀 시 재탐색)
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

            PlayerReturnContext.SetReturnFromTrigger(
                returnSceneName: curScene,
                returnPosition: pos,
                graceSeconds: graceSeconds,

                requestCameraRebind: requestCameraRebind,
                targetVcamName: worldVcamName,

                useOverlapSuppression: useOverlapSuppression,
                overlapRadius: suppressOverlapRadius,
                overlapSeconds: suppressOverlapSeconds,

                // ★ 카메라 복원 저장
                restoreCameraState: restoreCam,
                cameraMode: camMode,
                cameraOrthoSize: camOrtho,
                cameraFixedPos: camFixedPos,
                cameraBoundsName: camBoundsName
            );

            if (debugLog)
            {
                Debug.Log(
                    $"[TriggerStep_Scene] Saved Return: scene='{curScene}', pos=({pos.x:F2},{pos.y:F2}) " +
                    $"overlap(r={suppressOverlapRadius:F2}, sec={suppressOverlapSeconds:F2}) " +
                    $"camRestore={restoreCam} camMode={camMode} camOrtho={camOrtho:F2} camBounds='{camBoundsName}' camFixed=({camFixedPos.x:F2},{camFixedPos.y:F2})"
                );
            }
        }

        // Runner는 Single 전환에서만
        if (useSceneVisitEffectRunner && loadMode == LoadSceneMode.Single)
        {
            var runner = ResolveRunner();
            if (runner != null)
            {
                if (debugLog) Debug.Log("[TriggerStep_Scene] Runner -> LoadSceneWithExitEffect()");
                runner.LoadSceneWithExitEffect(sceneName);
                yield break;
            }
        }

        SceneManager.LoadScene(sceneName, loadMode);
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
