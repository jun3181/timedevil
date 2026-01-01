// SaveButtonHandler.cs
using UnityEngine;

public class SaveButtonHandler : MonoBehaviour
{
    // 카드만 저장
    public void SaveCardsOnly()
    {
        if (CardStateRuntime.Instance != null)
        {
            CardStateRuntime.Instance.SaveNow(); // 내부에서 card_state.json 등으로 저장
            Debug.Log("[SaveButtonHandler] Saved Cards");
        }
        else
        {
            Debug.LogWarning("[SaveButtonHandler] CardStateRuntime.Instance is null");
        }
    }

    // 플레이어만 저장
    public void SavePlayerOnly()
    {
        if (PlayerDataRuntime.Instance != null)
        {
            PlayerDataRuntime.Instance.SaveNow(); // 내부에서 player_data.json 등으로 저장
            Debug.Log("[SaveButtonHandler] Saved PlayerData");
        }
        else
        {
            Debug.LogWarning("[SaveButtonHandler] PlayerDataRuntime.Instance is null");
        }
    }

    // 둘 다 저장
    public void SaveAll()
    {
        SaveCardsOnly();
        SavePlayerOnly();
        Debug.Log("[SaveButtonHandler] Saved Cards + PlayerData");
    }
}
