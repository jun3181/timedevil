using UnityEngine;

/// <summary>
/// 상호작용 엔트리 포인트:
/// - Interact()가 호출되면 대화 시작
/// - 이 오브젝트의 레이어가 "item_get"이면 아이템(카드) 획득 처리:
/// - 카드ID 결정(기본: gameObject.name, 필요 시 overrideCardId)
/// - CardStateRuntime에 AddOwned(cardId) 호출 (저장은 하지 않음)
/// - (옵션) 획득 후 비활성화/파괴
/// </summary>
public class ObjectInteraction : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public Dialogue dialogue;  // 기존 대화 데이터 (선택)

    [Header("Item Pickup (for layer \"item_get\")")]
    [Tooltip("기본은 이 오브젝트의 name을 카드ID로 사용. 값을 넣으면 이 값으로 대체")]
    [SerializeField] private string overrideCardId = "";

    [Tooltip("아이템 획득 시 이 오브젝트를 비활성화할지")]
    [SerializeField] private bool disableAfterPickup = true;

    [Tooltip("아이템 획득 시 이 오브젝트를 파괴할지 (disable보다 우선)")]
    [SerializeField] private bool destroyAfterPickup = false;

    [Tooltip("획득 사운드(선택)")]
    [SerializeField] private AudioSource pickupSfx;

    /// <summary>
    /// 플레이어가 E키 등으로 호출하는 진입점
    /// </summary>
    public void Interact()
    {
        // 1) 대화 시작 (있으면)
        if (dialogue != null && DialogueManager.instance != null)
        {
            DialogueManager.instance.StartDialogue(dialogue);
        }

        // 2) 레이어가 아이템이면 획득 처리
        if (gameObject.layer == LayerMask.NameToLayer("item_get"))
        {
            HandleItemPickup();
        }
    }

    // -------------------- Helpers --------------------

    void HandleItemPickup()
    {
        string cardId = string.IsNullOrEmpty(overrideCardId) ? gameObject.name : overrideCardId;

        var cardState = FindObjectOfType<CardStateRuntime>();
        if (cardState == null)
        {
            Debug.LogWarning("[ObjectInteraction] CardStateRuntime이 씬에 없음. 카드 등록 불가");
            return;
        }

        // 저장은 하지 않음(런타임 메모리만) — 요구사항 그대로
        bool added = cardState.AddOwned(cardId);
        if (added)
        {
            Debug.Log($"[ObjectInteraction] 카드 획득(메모리만): {cardId}");

            if (pickupSfx != null)
                pickupSfx.Play();

            /*// 획득 후 오브젝트 처리
            if (destroyAfterPickup)
            {
                Destroy(gameObject);
            }
            else if (disableAfterPickup)
            {
                gameObject.SetActive(false);
            }*/
        }
        else
        {
            Debug.Log($"[ObjectInteraction] 이미 보유 중이라 등록 생략: {cardId}");
        }
    }
}
