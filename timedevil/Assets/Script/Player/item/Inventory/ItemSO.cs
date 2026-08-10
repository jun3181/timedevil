using UnityEngine;

public enum ItemType
{
    HealthRecovery,
    AttackBoost,
    DefenseBoost,
    SpeedBoost,
    Quest
}

[CreateAssetMenu(menuName = "Items/Item", fileName = "NewItem")]
public class ItemSO : ScriptableObject
{
    [Header("ID & Meta")]
    public string id;
    public string displayName;
    public ItemType type;

    [Header("Description & Visual")]
    [TextArea] public string description;
    public Sprite icon;

    [Header("Default Quantity")]
    public int defaultQuantity = 0;

    [Header("Built-in Effect")]
    [Min(0)] public int effectAmount = 0;
    public bool consumeOnUse = true;

    [Header("Custom Script (Optional)")]
    public ItemScriptBase itemScript;

    public bool CanUseFromInventory(out string message)
    {
        message = "";

        if (type == ItemType.Quest)
        {
            message = $"{GetNameForLog()} is a quest item.";
            return false;
        }

        if (itemScript != null)
            return itemScript.CanItemUsed(out message);

        if (effectAmount <= 0)
        {
            message = $"{GetNameForLog()} has no effect amount.";
            return false;
        }

        PlayerData player = GetPlayerData(out message);
        if (player == null)
            return false;

        if (type == ItemType.HealthRecovery && player.currentHP >= player.maxHP)
        {
            message = "HP is already full.";
            return false;
        }

        return true;
    }

    public bool TryUse(out string message)
    {
        if (!CanUseFromInventory(out message))
            return false;

        if (itemScript != null)
        {
            itemScript.Run();
            return true;
        }

        PlayerData player = GetPlayerData(out message);
        if (player == null)
            return false;

        switch (type)
        {
            case ItemType.HealthRecovery:
                player.currentHP = Mathf.Clamp(player.currentHP + effectAmount, 0, Mathf.Max(1, player.maxHP));
                return true;

            case ItemType.AttackBoost:
                player.attack = Mathf.Max(0, player.attack + effectAmount);
                return true;

            case ItemType.DefenseBoost:
                player.defense = Mathf.Max(0, player.defense + effectAmount);
                return true;

            case ItemType.SpeedBoost:
                player.speed = Mathf.Max(1, player.speed + effectAmount);
                return true;

            default:
                message = $"{GetNameForLog()} cannot be used from inventory.";
                return false;
        }
    }

    private string GetNameForLog()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return string.IsNullOrWhiteSpace(id) ? name : id;
    }

    private static PlayerData GetPlayerData(out string message)
    {
        message = "";

        if (PlayerDataRuntime.Instance == null || PlayerDataRuntime.Instance.Data == null)
        {
            message = "PlayerDataRuntime is missing.";
            return null;
        }

        return PlayerDataRuntime.Instance.Data;
    }
}
