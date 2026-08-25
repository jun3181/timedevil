using System;
using UnityEngine;

public enum SceneArrivalKind
{
    None,
    DefaultSceneStart,
    SpawnKey,
    WorldPosition,
    BattleReturn,
    ProgressLoad,
    MyroomEntry
}

[Serializable]
public struct SceneCameraRequest
{
    public bool hasCamera;
    public CameraModeId mode;
    public float orthoSize;
    public Vector3 fixedPosition;
    public string boundsName;
    public string preferredVcamName;

    public static SceneCameraRequest None => new SceneCameraRequest
    {
        hasCamera = false,
        mode = CameraModeId.FollowFree,
        orthoSize = 0f,
        fixedPosition = Vector3.zero,
        boundsName = null,
        preferredVcamName = null
    };

    public static SceneCameraRequest FromSnapshot(
        CameraModeId mode,
        float orthoSize,
        Vector3 fixedPosition,
        string boundsName,
        string preferredVcamName = null)
    {
        return new SceneCameraRequest
        {
            hasCamera = true,
            mode = mode,
            orthoSize = orthoSize,
            fixedPosition = fixedPosition,
            boundsName = string.IsNullOrWhiteSpace(boundsName) ? null : boundsName,
            preferredVcamName = string.IsNullOrWhiteSpace(preferredVcamName) ? null : preferredVcamName
        };
    }
}

[Serializable]
public class SceneArrivalRequest
{
    public string targetSceneName;
    public SceneArrivalKind kind;

    public string spawnKey;
    public MyroomEntryPoint myroomEntryPoint = MyroomEntryPoint.None;

    public bool hasWorldPosition;
    public Vector3 worldPosition;
    public bool keepPlayerZ = true;

    public SceneCameraRequest camera = SceneCameraRequest.None;

    public float graceSeconds;
    public bool requestCameraRebind;
    public string targetVcamName;

    public bool useOverlapSuppression;
    public float overlapRadius;
    public float overlapSeconds;

    public bool restoreEnemySnapshot;
    public string enemyInstanceId;
    public string enemyNameInScene;

    public static SceneArrivalRequest Default(string targetSceneName)
    {
        return new SceneArrivalRequest
        {
            targetSceneName = targetSceneName,
            kind = SceneArrivalKind.DefaultSceneStart
        };
    }

    public static SceneArrivalRequest SpawnKey(
        string targetSceneName,
        string spawnKey,
        SceneArrivalKind kind = SceneArrivalKind.SpawnKey)
    {
        return new SceneArrivalRequest
        {
            targetSceneName = targetSceneName,
            kind = kind,
            spawnKey = spawnKey
        };
    }

    public static SceneArrivalRequest WorldPosition(
        string targetSceneName,
        Vector3 position,
        SceneArrivalKind kind = SceneArrivalKind.WorldPosition)
    {
        return new SceneArrivalRequest
        {
            targetSceneName = targetSceneName,
            kind = kind,
            hasWorldPosition = true,
            worldPosition = position
        };
    }

    public static SceneArrivalRequest Myroom(string targetSceneName, MyroomEntryPoint entryPoint)
    {
        return new SceneArrivalRequest
        {
            targetSceneName = targetSceneName,
            kind = SceneArrivalKind.MyroomEntry,
            myroomEntryPoint = entryPoint,
            spawnKey = entryPoint.ToString()
        };
    }
}
