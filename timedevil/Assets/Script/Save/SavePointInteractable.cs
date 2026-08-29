using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SavePointInteractable : MonoBehaviour, IInteractable
{
    [Header("Optional SFX")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip saveClip;

    [Header("Progress Save (Scene/Pos/Camera)")]
    [Tooltip("If set, saves this scene name to progress.json lastSceneName. Otherwise uses current scene name.")]
    [SerializeField] private string overrideSceneName = "";

    [Tooltip("Save player position to progress.json")]
    [SerializeField] private bool savePlayerPosition = true;

    [Header("Developer Overrides (Priority)")]
    [Tooltip("Use developer-defined transforms before runtime positions when saving.")]
    [SerializeField] private bool preferDeveloperOverrides = true;

    [Tooltip("Override player save position. If empty, uses runtime player position.")]
    [SerializeField] private Transform playerPositionOverride;

    [Header("Camera Save")]
    [SerializeField] private bool saveCamera = true;

    [Tooltip("Saves the CameraManager mode/position/ortho that is active at the save moment.")]
    [SerializeField] private bool captureCurrentCameraOnSave = true;

    [Header("Camera Save Fallback (Inspector Override)")]
    [SerializeField] private CameraModeId cameraMode = CameraModeId.FollowFree;

    [Tooltip("0 keeps CameraManager default size")]
    [SerializeField] private float orthoSize = 0f;

    [Tooltip("Fixed/Cutscene camera anchor. If empty, uses player position fallback.")]
    [SerializeField] private Transform fixedCameraAnchor;

    [Tooltip("Bounds for FollowConfined mode (resolved by name on load)")]
    [SerializeField] private Collider2D confinerBounds;

    [Header("Save Complete Popup")]
    [SerializeField] private bool showSaveCompletePopup = true;
    [SerializeField] private string saveCompleteMessage = "저장완료!";
    [SerializeField] private KeyCode saveCompleteCloseKey = KeyCode.E;
    [SerializeField] private bool lockPlayerInputWhilePopup = true;
    [SerializeField] private TMP_FontAsset popupFont;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public void Interact()
    {
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        if (gameObject.layer != LayerMask.NameToLayer("Save") && debugLog)
            Debug.LogWarning($"[SavePoint] '{name}' is not on 'Save' layer (but still saving).");

        // 1) Progress save
        SaveProgressOnly();

        // 2) Other saves
        SaveSystem.SaveCards();
        SaveSystem.SaveItems();
        SaveSystem.SavePlayerData();

        if (sfx && saveClip) sfx.PlayOneShot(saveClip);

        if (showSaveCompletePopup)
            SaveCompletePopup.Show(saveCompleteMessage, saveCompleteCloseKey, popupFont, lockPlayerInputWhilePopup);

        if (debugLog) Debug.Log("[SavePoint] Saved!");
    }

    private void SaveProgressOnly()
    {
        var data = ProgressSaveStore.Load();

        data.lastSceneName = !string.IsNullOrEmpty(overrideSceneName)
            ? overrideSceneName
            : SceneManager.GetActiveScene().name;

        data.unixTimeUtc = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (savePlayerPosition)
        {
            if (preferDeveloperOverrides && playerPositionOverride != null)
            {
                data.playerPos = playerPositionOverride.position;
            }
            else
            {
                var p = SaveSystem.ResolvePlayerTransform();
                if (p != null) data.playerPos = p.position;
            }
        }

        if (saveCamera)
        {
            if (!captureCurrentCameraOnSave || !TrySaveCurrentCameraSnapshot(data))
                SaveInspectorCameraFallback(data);
        }
        else
        {
            data.hasCamera = false;
            data.cameraBoundsName = null;
            data.cameraOrthoSize = 0f;
        }

        data.triggerRuntime = TriggerRuntimeSaveBridge.Capture();
        ProgressSaveStore.Save(data);
    }

    private bool TrySaveCurrentCameraSnapshot(ProgressSaveData data)
    {
        var cm = CameraManager.Instance ?? FindObjectOfType<CameraManager>(true);
        if (cm == null)
        {
            if (debugLog) Debug.LogWarning("[SavePoint] CameraManager not found. Using inspector camera fallback.");
            return false;
        }

        if (!cm.TryGetSnapshot(out CameraModeId currentMode, out float currentOrtho, out Vector3 currentFixedPos, out string currentBoundsName))
        {
            if (debugLog) Debug.LogWarning("[SavePoint] CameraManager snapshot failed. Using inspector camera fallback.");
            return false;
        }

        data.hasCamera = true;
        data.cameraMode = currentMode;
        data.cameraOrthoSize = currentOrtho;
        data.cameraFixedPos = currentFixedPos;
        data.cameraBoundsName = string.IsNullOrWhiteSpace(currentBoundsName) ? null : currentBoundsName.Trim();

        if (debugLog)
        {
            string boundsText = string.IsNullOrWhiteSpace(data.cameraBoundsName) ? "(none)" : data.cameraBoundsName;
            Debug.Log($"[SavePoint] Camera snapshot saved: mode={data.cameraMode}, ortho={data.cameraOrthoSize:F2}, fixed={data.cameraFixedPos}, bounds='{boundsText}'");
        }

        return true;
    }

    private void SaveInspectorCameraFallback(ProgressSaveData data)
    {
        data.hasCamera = true;
        data.cameraMode = cameraMode;
        data.cameraOrthoSize = orthoSize;

        Vector3 fixedPos;
        if (fixedCameraAnchor != null)
        {
            fixedPos = fixedCameraAnchor.position;
        }
        else if (preferDeveloperOverrides && playerPositionOverride != null)
        {
            fixedPos = playerPositionOverride.position;
        }
        else
        {
            var p = SaveSystem.ResolvePlayerTransform();
            fixedPos = (p != null) ? p.position : Vector3.zero;
        }

        data.cameraFixedPos = fixedPos;
        data.cameraBoundsName = (confinerBounds != null) ? confinerBounds.name : null;
    }
}
