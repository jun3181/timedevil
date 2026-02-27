using UnityEngine;

public static class SaveSystem
{
    public static void ClearAllSaves()
    {
        CardSaveStore.DeleteSave();
        ItemSaveStore.Delete("items_save.json");
        PlayerDataStore.DeleteSave();
        ProgressSaveStore.DeleteSave();

        Debug.Log("[SaveSystem] Cleared all save files for NewGame.");
    }

    // -------------------------
    // Cards
    // -------------------------
    public static void SaveCards()
    {
        if (CardStateRuntime.Instance != null)
            CardStateRuntime.Instance.SaveNow();   // cards.json
        else
            Debug.LogWarning("[SaveSystem] CardStateRuntime.Instance not found. cards skip.");
    }

    // -------------------------
    // Items
    // -------------------------
    public static void SaveItems()
    {
        if (ItemRuntime.Instance != null)
            ItemRuntime.Instance.SaveToDisk();     // items_save.json
        else
            Debug.LogWarning("[SaveSystem] ItemRuntime.Instance not found. items skip.");
    }

    // -------------------------
    // PlayerData
    // -------------------------
    public static void SavePlayerData()
    {
        if (PlayerDataRuntime.Instance != null)
            PlayerDataRuntime.Instance.SaveNow();  // player.json (PlayerData 런타임)
        else
            Debug.LogWarning("[SaveSystem] PlayerDataRuntime.Instance not found. player data skip.");
    }

    // -------------------------
    // Helpers
    // -------------------------
    public static Transform ResolvePlayerTransform()
    {
        var pmm = UnityEngine.Object.FindObjectOfType<PlayerMainManager>(true);
        if (pmm) return pmm.transform;

        var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>(true);
        if (pm) return pm.transform;

        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }
}
