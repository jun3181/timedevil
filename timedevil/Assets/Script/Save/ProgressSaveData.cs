using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProgressSaveData
{
    // 마지막 저장된 씬(꿈/챕터)
    public string lastSceneName = "";

    // 체크포인트(저장 시점) 플레이어 위치
    public Vector3 playerPos;

    // 마지막 저장 시간(UTC unix seconds)
    public long unixTimeUtc = 0;

    // -------------------------
    // Camera (저장 오브젝트 인스펙터에서 "어떤 카메라 상태를 저장할지" 지정)
    // -------------------------
    public bool hasCamera = false;
    public CameraModeId cameraMode = CameraModeId.FollowFree;

    // 0이면 CameraManager 기본값 유지
    public float cameraOrthoSize = 0f;

    // Fixed/Cutscene일 때 고정 위치
    public Vector3 cameraFixedPos;

    // FollowConfined일 때 bounds 콜라이더 이름(로드 시 이름으로 재탐색)
    public string cameraBoundsName = null;

    // -------------------------
    // Flags (컷씬/트리거 "키 받았나?" 체크)
    // -------------------------
    public List<string> flags = new List<string>();

    // -------------------------
    // Trigger runtime snapshot at save point
    // -------------------------
    public TriggerRuntimeSaveData triggerRuntime = new TriggerRuntimeSaveData();

    public bool HasFlag(string key)
        => !string.IsNullOrEmpty(key) && flags != null && flags.Contains(key);

    public void AddFlag(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (flags == null) flags = new List<string>();
        if (!flags.Contains(key)) flags.Add(key);
    }
}

[Serializable]
public class TriggerRuntimeSaveData
{
    public TriggerComponentSaveData triggerGet = new TriggerComponentSaveData();
    public TriggerComponentSaveData interaction = new TriggerComponentSaveData();
}

[Serializable]
public class TriggerComponentSaveData
{
    public List<TriggerCallCountEntry> callCounts = new List<TriggerCallCountEntry>();
    public List<TriggerStageProgressEntry> stageProgress = new List<TriggerStageProgressEntry>();
    public List<string> completedIds = new List<string>();
}

[Serializable]
public class TriggerCallCountEntry
{
    public string id;
    public int callCount;
}

[Serializable]
public class TriggerStageProgressEntry
{
    public string id;
    public int stageIndex;
    public int callCount;
}
