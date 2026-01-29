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

            Vector2 pos;
            if (returnPointOverride != null)
            {
                pos = returnPointOverride.position;
            }
            else
            {
                var player = Object.FindObjectOfType<PlayerMainManager>(true);
                pos = player ? (Vector2)player.transform.position : Vector2.zero;
            }

            PlayerReturnContext.SetReturnFromTrigger(
                returnSceneName: curScene,
                returnPosition: pos,
                graceSeconds: graceSeconds,
                requestCameraRebind: requestCameraRebind,
                targetVcamName: worldVcamName,
                useOverlapSuppression: useOverlapSuppression,
                overlapRadius: suppressOverlapRadius,
                overlapSeconds: suppressOverlapSeconds
            );

            if (debugLog)
                Debug.Log($"[TriggerStep_Scene] Saved Return: scene='{curScene}', pos=({pos.x:F2},{pos.y:F2}) overlap(r={suppressOverlapRadius:F2}, sec={suppressOverlapSeconds:F2})");
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
