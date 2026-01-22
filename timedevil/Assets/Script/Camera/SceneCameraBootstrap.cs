using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneCameraBootstrap : MonoBehaviour
{
    [Header("Start Mode")]
    public CameraModeId startMode = CameraModeId.Fixed;

    [Header("Follow Target (비우면 PlayerMove 자동 탐색)")]
    public Transform followTarget;

    [Header("Confiner (FollowConfined에서 사용)")]
    public Collider2D confinerBounds;

    [Header("Zoom")]
    public float orthoSize = 5f;

    [Header("Fixed/Cutscene Position (선택)")]
    public Transform fixedOrCutsceneAnchor;

    [Header("Debug")]
    public bool debugLog = true;

    private void OnEnable()
    {
        // 씬 로드 직후 오브젝트들 활성화 끝나고 적용(1프레임 뒤)
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ApplyNextFrame());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        StartCoroutine(ApplyNextFrame());
    }

    private IEnumerator ApplyNextFrame()
    {
        yield return null;

        if (!CameraManager.Instance) yield break;

        var target = followTarget;
        if (!target)
        {
            var pm = FindObjectOfType<PlayerMove>(true);
            if (pm) target = pm.transform;
        }

        if (debugLog) Debug.Log($"[SceneCameraBootstrap] Apply startMode={startMode} target={(target ? target.name : "(null)")} confiner={(confinerBounds ? confinerBounds.name : "(null)")} ortho={orthoSize}");

        switch (startMode)
        {
            case CameraModeId.Fixed:
                CameraManager.Instance.SetFixed(
                    lockWorldPos: fixedOrCutsceneAnchor ? fixedOrCutsceneAnchor.position : (Vector3?)null,
                    orthoSize: orthoSize,
                    disableConfiner: true
                );
                break;

            case CameraModeId.FollowConfined:
                CameraManager.Instance.SetFollowConfined(
                    followTarget: target,
                    bounds: confinerBounds,
                    orthoSize: orthoSize
                );
                break;

            case CameraModeId.FollowFree:
                CameraManager.Instance.SetFollowFree(
                    followTarget: target,
                    orthoSize: orthoSize
                );
                break;

            case CameraModeId.Cutscene:
                var pos = fixedOrCutsceneAnchor ? fixedOrCutsceneAnchor.position : Camera.main.transform.position;
                CameraManager.Instance.SetCutscene(
                    worldPos: pos,
                    orthoSize: orthoSize,
                    useConfiner: false,
                    bounds: null
                );
                break;
        }
    }
}
