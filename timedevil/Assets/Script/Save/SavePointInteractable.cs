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

    [Header("Camera Save (Inspector Override)")]
    [SerializeField] private bool saveCamera = true;
    [SerializeField] private CameraModeId cameraMode = CameraModeId.FollowFree;

    [Tooltip("0 keeps CameraManager default size")]
    [SerializeField] private float orthoSize = 0f;

    [Tooltip("Fixed/Cutscene camera anchor. If empty, uses player position fallback.")]
    [SerializeField] private Transform fixedCameraAnchor;

    [Tooltip("Bounds for FollowConfined mode (resolved by name on load)")]
    [SerializeField] private Collider2D confinerBounds;

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
        else
        {
            data.hasCamera = false;
            data.cameraBoundsName = null;
            data.cameraOrthoSize = 0f;
        }

        ProgressSaveStore.Save(data);
    }
}
