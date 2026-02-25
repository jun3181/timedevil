using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-19000)]
[DisallowMultipleComponent]
public class ProgressLoadApplier : MonoBehaviour
{
    [Header("Policy")]
    [Tooltip("true면 '침대 로드'로 들어온 경우에만 적용")]
    [SerializeField] private bool applyOnlyWhenSleepLoad = true;

    [SerializeField] private bool applyPlayerPosition = true;
    [SerializeField] private bool applyCamera = true;

    [Header("Player Find")]
    [SerializeField] private int maxFindPlayerFrames = 60;
    [SerializeField] private bool keepPlayerZ = true;

    [Header("Action Lock Safety")]
    [SerializeField] private bool forceClearActionLocksOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private IEnumerator Start()
    {
        if (applyOnlyWhenSleepLoad)
        {
            if (!SleepLoadContext.Consume())
            {
                if (debugLog) Debug.Log("[ProgressLoadApplier] skip (not sleep-load)");
                yield break;
            }
        }

        if (forceClearActionLocksOnStart && GameManager.Instance != null)
            GameManager.Instance.ForceClearActionLocks();

        var data = ProgressSaveStore.Load();
        string curScene = SceneManager.GetActiveScene().name;

        // 세이브된 씬이 현재 씬이 아니면 적용하지 않음(실수 방지)
        if (!string.IsNullOrEmpty(data.lastSceneName) && data.lastSceneName != curScene)
        {
            if (debugLog)
                Debug.Log($"[ProgressLoadApplier] skip (save scene='{data.lastSceneName}', current='{curScene}')");
            yield break;
        }

        // Player 찾기
        Transform player = null;
        for (int i = 0; i < maxFindPlayerFrames; i++)
        {
            player = SaveSystem.ResolvePlayerTransform();
            if (player != null) break;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning("[ProgressLoadApplier] Player not found.");
            yield break;
        }

        // 1) 위치 적용
        if (applyPlayerPosition)
        {
            Vector3 pos = data.playerPos;
            if (keepPlayerZ) pos.z = player.position.z;
            player.position = pos;

            if (debugLog) Debug.Log($"[ProgressLoadApplier] playerPos -> {pos}");
        }

        // 2) 카메라 적용
        if (applyCamera && data.hasCamera && CameraManager.Instance != null)
        {
            // follow 대상은 기본 Player
            Transform follow = player;

            float? ortho = (data.cameraOrthoSize > 0f) ? data.cameraOrthoSize : (float?)null;

            // bounds는 이름으로 찾아옴
            Collider2D bounds = null;
            if (!string.IsNullOrWhiteSpace(data.cameraBoundsName))
            {
                var all = FindObjectsOfType<Collider2D>(true);
                foreach (var c in all)
                {
                    if (c != null && c.name == data.cameraBoundsName) { bounds = c; break; }
                }
            }

            if (debugLog)
                Debug.Log($"[ProgressLoadApplier] camera -> mode={data.cameraMode}, ortho={(ortho.HasValue ? ortho.Value.ToString("F2") : "(default)")}, bounds='{data.cameraBoundsName}' found={(bounds ? bounds.name : "(null)")}, fixed={data.cameraFixedPos}");

            switch (data.cameraMode)
            {
                case CameraModeId.Fixed:
                    CameraManager.Instance.SetFixed(data.cameraFixedPos, ortho);
                    CameraManager.Instance.SnapCameraTo(data.cameraFixedPos);
                    break;

                case CameraModeId.Cutscene:
                    CameraManager.Instance.SetCutscene(data.cameraFixedPos, ortho);
                    CameraManager.Instance.SnapCameraTo(data.cameraFixedPos);
                    break;

                case CameraModeId.FollowConfined:
                    if (follow != null)
                    {
                        if (bounds != null) CameraManager.Instance.SetFollowConfined(follow, bounds, ortho);
                        else CameraManager.Instance.SetFollowFree(follow, ortho);
                        CameraManager.Instance.NotifyTargetWarp(follow, Vector3.zero);
                    }
                    break;

                case CameraModeId.FollowFree:
                default:
                    if (follow != null)
                    {
                        CameraManager.Instance.SetFollowFree(follow, ortho);
                        CameraManager.Instance.NotifyTargetWarp(follow, Vector3.zero);
                    }
                    break;
            }
        }
    }
}