// Assets/Script/Save/ProgressSaveData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProgressSaveData
{
    // 마지막 저장된 씬(꿈/챕터 등)
    public string lastSceneName = "";

    // 마지막 저장된 플레이어 월드 좌표
    public Vector3 playerPos;

    // ✅ 카메라 복원 (저장 오브젝트 인스펙터에서 지정한 값)
    public CameraModeId cameraMode = CameraModeId.FollowFree;
    public float cameraOrthoSize = 0f;       // 0이면 CameraManager 기본값 유지
    public Vector3 cameraFixedPos;           // Fixed/Cutscene일 때 사용
    public string cameraBoundsName = "";     // FollowConfined일 때 bounds 이름 저장(복귀 시 재탐색)

    // “봤음/해금/컷씬키 수령” 등
    public List<string> flags = new List<string>();

    public bool HasFlag(string key) => flags != null && flags.Contains(key);

    public void AddFlag(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (flags == null) flags = new List<string>();
        if (!flags.Contains(key)) flags.Add(key);
    }
}