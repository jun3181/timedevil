// Assets/Script/Save/SaveSystem.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    // SavePoint에서 “인스펙터로 지정한 저장값”을 넘기기 위한 요청 구조
    public struct SaveRequest
    {
        // 진행/좌표/씬
        public bool overrideSceneName;
        public string sceneName;

        public bool overridePlayerPos;
        public Vector3 playerPos;

        // 컷씬 키/플래그
        public bool addFlag;
        public string flagKey;

        // 카메라(인스펙터 지정값)
        public bool overrideCamera;
        public CameraModeId cameraMode;
        public float cameraOrthoSize;
        public Vector3 cameraFixedPos;
        public string cameraBoundsName;
    }

    public static void SaveAll() => SaveAll(default);

    public static void SaveAll(SaveRequest req)
    {
        SaveProgress(req);
        SaveCards();
        SaveItems();
        SavePlayer();

#if UNITY_EDITOR
        Debug.Log($"[SaveSystem] SaveAll complete. root={Application.persistentDataPath}");
#endif
    }

    // -------------------------
    // 1) 진행상황(progress.json) : 여기만 “씬/좌표/카메라/키” 저장
    // -------------------------
    private static void SaveProgress(SaveRequest req)
    {
        var data = ProgressSaveStore.Load();

        // 씬 이름
        if (req.overrideSceneName && !string.IsNullOrEmpty(req.sceneName))
            data.lastSceneName = req.sceneName;
        else
            data.lastSceneName = SceneManager.GetActiveScene().name;

        // 플레이어 좌표
        Vector3 pos;
        if (req.overridePlayerPos)
            pos = req.playerPos;
        else
        {
            var p = ResolvePlayerTransform();
            pos = p ? p.position : Vector3.zero;
        }
        data.playerPos = pos;

        // 플래그/키
        if (req.addFlag && !string.IsNullOrEmpty(req.flagKey))
            data.AddFlag(req.flagKey);

        // 카메라 저장(인스펙터 지정값)
        if (req.overrideCamera)
        {
            data.cameraMode = req.cameraMode;
            data.cameraOrthoSize = req.cameraOrthoSize;
            data.cameraFixedPos = req.cameraFixedPos;
            data.cameraBoundsName = req.cameraBoundsName ?? "";
        }

        ProgressSaveStore.Save(data);
    }

    // -------------------------
    // 2) 카드(cards.json)
    // -------------------------
    private static void SaveCards()
    {
        if (CardStateRuntime.Instance != null) CardStateRuntime.Instance.SaveNow();
        else Debug.LogWarning("[SaveSystem] CardStateRuntime.Instance not found. cards skip.");
    }

    // -------------------------
    // 3) 아이템(items_save.json)
    // -------------------------
    private static void SaveItems()
    {
        if (ItemRuntime.Instance != null) ItemRuntime.Instance.SaveToDisk();
        else Debug.LogWarning("[SaveSystem] ItemRuntime.Instance not found. items skip.");
    }

    // -------------------------
    // 4) 플레이어 스탯(player.json) : PlayerDataRuntime에게만 맡김
    // -------------------------
    private static void SavePlayer()
    {
        if (PlayerDataRuntime.Instance != null) PlayerDataRuntime.Instance.SaveNow();
        else Debug.LogWarning("[SaveSystem] PlayerDataRuntime.Instance not found. player skip.");
    }

    private static Transform ResolvePlayerTransform()
    {
        var pmm = UnityEngine.Object.FindObjectOfType<PlayerMainManager>(true);
        if (pmm) return pmm.transform;

        var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>(true);
        if (pm) return pm.transform;

        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }
}