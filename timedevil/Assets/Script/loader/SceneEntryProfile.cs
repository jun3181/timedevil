using System;
using UnityEngine;

[Serializable]
public class SceneEntryDefinition
{
    [Header("Entry")]
    public string key = "";
    public Transform spawnPoint;
    public bool keepPlayerZ = true;

    [Header("Camera")]
    public bool applyCamera = false;
    public CameraModeId cameraMode = CameraModeId.FollowFree;
    public float orthoSize = 0f;
    public Transform fixedCameraAnchor;
    public Collider2D confinerBounds;
    public string preferredVcamName = "";

    public bool HasSpawn => spawnPoint != null;

    public SceneCameraRequest ToCameraRequest(Vector3 fallbackPosition)
    {
        if (!applyCamera)
            return SceneCameraRequest.None;

        Vector3 fixedPosition = fixedCameraAnchor != null
            ? fixedCameraAnchor.position
            : fallbackPosition;

        return SceneCameraRequest.FromSnapshot(
            cameraMode,
            orthoSize,
            fixedPosition,
            confinerBounds != null ? confinerBounds.name : null,
            preferredVcamName
        );
    }
}

[DisallowMultipleComponent]
public class SceneEntryProfile : MonoBehaviour
{
    [Header("Default Entry")]
    [SerializeField] private bool useDefaultEntry = false;
    [SerializeField] private SceneEntryDefinition defaultEntry = new SceneEntryDefinition();

    [Header("Named Entries")]
    [SerializeField] private SceneEntryDefinition[] entries = new SceneEntryDefinition[0];

    public bool TryGetDefault(out SceneEntryDefinition entry)
    {
        entry = null;
        if (!useDefaultEntry || defaultEntry == null) return false;
        entry = defaultEntry;
        return true;
    }

    public bool TryGetEntry(string key, out SceneEntryDefinition entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(key) || entries == null) return false;

        string normalizedKey = key.Trim();
        for (int i = 0; i < entries.Length; i++)
        {
            var candidate = entries[i];
            if (candidate == null) continue;
            if (string.Equals(candidate.key?.Trim(), normalizedKey, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }
}
