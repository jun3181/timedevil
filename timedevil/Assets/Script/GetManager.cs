using UnityEngine;
using TMPro;

public class GetManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI talkText;
    public GameObject talkPanel;

    private GameObject scanObject;
    public bool isAction;

    public void Action(GameObject scanObj)
    {
        if (scanObj == null || talkText == null || talkPanel == null)
        {
            Debug.LogWarning("[GetManager] 필드가 비어있거나 scanObj가 null임");
            return;
        }

        if (!isAction)
        {
            isAction = true;
            scanObject = scanObj;
            talkText.text = $"{scanObj.name} 아이템 획득!";

            string cardId = scanObj.name;

            var cardState = FindObjectOfType<CardStateRuntime>();
            if (cardState != null)
            {
                if (cardState.AddOwned(cardId))
                {
                    Debug.Log($"[GetManager] 카드 등록 (메모리만): {cardId}");
                }
            }
            else
            {
                Debug.LogWarning("[GetManager] CardStateRuntime이 씬에 없음. 등록 불가");
            }
        }
        else
        {
            isAction = false;
        }

        talkPanel.SetActive(isAction);
    }
}
