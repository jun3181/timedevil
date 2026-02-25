using UnityEngine;
using UnityEngine.SceneManagement;

public class SavePointInteractable : MonoBehaviour, IInteractable
{
    [Header("Optional SFX")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip saveClip;

    [Header("Progress Save (Scene/Pos/Camera)")]
    [Tooltip("progress.json의 lastSceneName에 저장할 씬. 비우면 '현재 씬'")]
    [SerializeField] private string overrideSceneName = "";

    [Tooltip("플레이어 위치를 progress.json에 저장")]
    [SerializeField] private bool savePlayerPosition = true;

    [Header("Camera Save (Inspector Override)")]
    [SerializeField] private bool saveCamera = true;

    [SerializeField] private CameraModeId cameraMode = CameraModeId.FollowFree;

    [Tooltip("0이면 CameraManager 기본값 유지")]
    [SerializeField] private float orthoSize = 0f;

    [Tooltip("Fixed/Cutscene일 때 저장할 카메라 고정 위치. 비우면 플레이어 위치 사용")]
    [SerializeField] private Transform fixedCameraAnchor;

    [Tooltip("FollowConfined일 때 저장할 bounds. (이름으로 저장됨)")]
    [SerializeField] private Collider2D confinerBounds;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public void Interact()
    {
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        if (gameObject.layer != LayerMask.NameToLayer("Save") && debugLog)
            Debug.LogWarning($"[SavePoint] '{name}' is not on 'Save' layer (but still saving).");

        // 1) progress 저장
        SaveProgressOnly();

        // 2) 나머지 저장
        SaveSystem.SaveCards();
        SaveSystem.SaveItems();
        SaveSystem.SavePlayerData();

        if (sfx && saveClip) sfx.PlayOneShot(saveClip);

        if (debugLog) Debug.Log("[SavePoint] Saved!");
    }

    private void SaveProgressOnly()
    {
        var data = ProgressSaveStore.Load();

        // 씬
        data.lastSceneName = !string.IsNullOrEmpty(overrideSceneName)
            ? overrideSceneName
            : SceneManager.GetActiveScene().name;

        data.unixTimeUtc = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 플레이어 좌표
        if (savePlayerPosition)
        {
            var p = SaveSystem.ResolvePlayerTransform();
            if (p != null) data.playerPos = p.position;
        }

        // 카메라(인스펙터 지정값 저장)
        if (saveCamera)
        {
            data.hasCamera = true;
            data.cameraMode = cameraMode;
            data.cameraOrthoSize = orthoSize;

            // Fixed/Cutscene 고정 위치
            Vector3 fixedPos = Vector3.zero;
            if (fixedCameraAnchor != null) fixedPos = fixedCameraAnchor.position;
            else
            {
                var p = SaveSystem.ResolvePlayerTransform();
                fixedPos = (p != null) ? p.position : Vector3.zero;
            }
            data.cameraFixedPos = fixedPos;

            // Confined bounds 이름 저장
            data.cameraBoundsName = (confinerBounds != null) ? confinerBounds.name : null;
        }
        else
        {
            data.hasCamera = false;
            data.cameraBoundsName = null;
            data.cameraOrthoSize = 0f;
        }

        ProgressSaveStore.Save(data);
    }
}