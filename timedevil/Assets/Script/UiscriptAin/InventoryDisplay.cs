using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;    // 🔥 TextMeshPro 사용

#region UI 슬롯 구조
[System.Serializable]
public class ItemSlotUI
{
    public Text nameText;      // NAME 열
    public Text quantityText;  // QTY 열
    public Image iconImage;    // ITEM 칸의 아이콘 이미지 (ItemDesc 오브젝트의 Image)

    // 🔥 이 슬롯에 어떤 아이템이 들어있는지 기억
    [HideInInspector] public ItemSO currentItemSO;
}
#endregion

public class InventoryDisplay : MonoBehaviour
{
    // 🔥 어디서든 확인 가능한 "설명창 열려 있음" 상태 플래그
    public static bool IsAnyDescriptionOpen { get; private set; } = false;

    [Header("슬롯 6개 연결 (Inspector에서 드래그)")]
    public ItemSlotUI[] slots;

    [Header("데이터 소스 (런타임 인벤토리)")]
    public InventoryDataSource dataSource;   // 🔥 ItemRuntime.CurrentData 래핑

    [Header("아이템 데이터베이스(SO)")]
    public ItemDataBaseSO itemDatabase;     // ItemDatabase SO 드래그

    [Header("페이지 설정")]
    [Tooltip("0=첫 페이지, 1=두 번째 페이지...")]
    [SerializeField] private int pageIndex = 0;
    [SerializeField] private int pageSize = 6;

    [Header("설명 패널")]
    public GameObject descriptionPanel;   // 설명 창 전체 (처음엔 비활성화)
    public TMP_Text descriptionText;      // 🔥 TMP용 설명 텍스트

    [Header("커서 참조")]
    public InventoryCursor cursor;        // 인벤토리 커서

    // 현재 페이지에서 사용 중인 (수량>0) 아이템 리스트
    private List<InventoryItemEntry> currentFiltered = new List<InventoryItemEntry>();
    private int currentStartIndex = 0;    // 이 페이지의 시작 인덱스 (디버그용 느낌)

    private void Start()
    {
        // 🔥 데이터 소스가 제대로 연결되었는지 확인
        if (dataSource == null)
        {
            Debug.LogError("❌ InventoryDisplay에 InventoryDataSource가 연결되어 있지 않습니다!");
        }

        DisplayCurrentPage();

        // 시작 시 설명 패널은 숨기기
        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);

        IsAnyDescriptionOpen = false;
    }

    private void OnDisable()
    {
        // 씬 전환/비활성화 시 잠금 풀어두기
        IsAnyDescriptionOpen = false;
    }

    private void Update()
    {
        // D 키를 INFO 버튼처럼 사용
        if (Input.GetKeyDown(KeyCode.D))
        {
            ToggleDescriptionPanel();
        }
    }

    /// <summary>현재 pageIndex 기준으로 페이지 내용을 슬롯에 표시</summary>
    public void DisplayCurrentPage()
    {
        // 🔥 데이터 소스에서 데이터 가져오기
        InventorySaveData inventoryData = (dataSource != null) ? dataSource.InventoryData : null;

        if (inventoryData == null || inventoryData.items == null)
        {
            Debug.LogWarning("⚠️ InventorySaveData가 비어 있습니다. 표시할 아이템이 없습니다.");
            ClearAllSlots();
            return;
        }

        // 1) 수량이 0보다 큰 아이템만 필터링
        currentFiltered.Clear();
        foreach (var e in inventoryData.items)
        {
            if (e != null && e.quantity > 0)
                currentFiltered.Add(e);
        }

        if (currentFiltered.Count == 0)
        {
            Debug.Log("ℹ️ 표시할 아이템이 없습니다. (모두 수량 0)");
            ClearAllSlots();
            return;
        }

        int totalCount = currentFiltered.Count;
        int start = Mathf.Max(0, pageIndex * pageSize);
        int end = Mathf.Min(start + pageSize, totalCount);

        currentStartIndex = start; // 현재 페이지 시작 인덱스 저장 (필요하면 디버그용으로 사용)

        for (int i = 0; i < slots.Length; i++)
        {
            int dataIdx = start + i;

            if (dataIdx >= start && dataIdx < end)
            {
                var entry = currentFiltered[dataIdx];

                // 2) ItemDatabase에서 정의 가져오기
                ItemSO def = itemDatabase != null
                    ? itemDatabase.GetById(entry.id)
                    : null;

                if (def == null)
                {
                    Debug.LogWarning($"⚠️ ItemDatabase에서 id '{entry.id}'에 해당하는 ItemSO를 찾지 못했습니다.");
                    ClearSlot(slots[i]);
                    continue;
                }

                string displayName = def.displayName;
                Sprite icon = def.icon;
                int quantity = entry.quantity; // 실제 수량은 JSON/런타임 기준

                // 🔥 이 슬롯이 어떤 아이템을 들고 있는지 기록
                slots[i].currentItemSO = def;

                if (slots[i].nameText) slots[i].nameText.text = displayName;
                if (slots[i].quantityText) slots[i].quantityText.text = $"x{quantity}";

                if (slots[i].iconImage)
                {
                    slots[i].iconImage.sprite = icon;
                    slots[i].iconImage.enabled = icon != null;
                }

                Debug.Log($"[InventoryDisplay p{pageIndex}] {entry.id} x{quantity}");
            }
            else
            {
                // 남는 칸 클리어
                ClearSlot(slots[i]);
            }
        }
    }

    /// <summary>D 키로 설명 패널 열고 닫기</summary>
    private void ToggleDescriptionPanel()
    {
        if (descriptionPanel == null || descriptionText == null) return;
        if (slots == null || slots.Length == 0) return;

        // 이미 열려 있으면 닫기
        if (descriptionPanel.activeSelf)
        {
            descriptionPanel.SetActive(false);
            IsAnyDescriptionOpen = false;
            return;
        }

        // 현재 커서가 가리키는 슬롯 인덱스 (0~5)
        int localIndex = (cursor != null) ? cursor.CurrentIndex : 0;

        if (localIndex < 0 || localIndex >= slots.Length)
        {
            descriptionPanel.SetActive(false);
            IsAnyDescriptionOpen = false;
            return;
        }

        var slot = slots[localIndex];
        var def = slot.currentItemSO;

        // 슬롯에 실제 아이템이 없으면 설명 안 뜨게
        if (def == null)
        {
            descriptionPanel.SetActive(false);
            IsAnyDescriptionOpen = false;
            return;
        }

        // 🔥 SO에 적힌 설명 사용
        descriptionText.text = def.description;
        descriptionPanel.SetActive(true);
        IsAnyDescriptionOpen = true;
    }

    /// <summary>외부(페이지 매니저)에서 페이지를 바꿀 때 호출</summary>
    public void SetPage(int newPageIndex)
    {
        if (newPageIndex < 0) newPageIndex = 0;
        pageIndex = newPageIndex;

        // 페이지 바뀔 때는 설명 패널도 꺼두기
        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);

        IsAnyDescriptionOpen = false;

        DisplayCurrentPage();
    }

    /// <summary>슬롯 전부 비우기</summary>
    private void ClearAllSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            ClearSlot(slots[i]);
        }
    }

    private void ClearSlot(ItemSlotUI slot)
    {
        if (slot == null) return;

        // 🔥 SO 정보도 같이 비우기
        slot.currentItemSO = null;

        if (slot.nameText) slot.nameText.text = "";
        if (slot.quantityText) slot.quantityText.text = "";
        if (slot.iconImage)
        {
            slot.iconImage.sprite = null;
            slot.iconImage.enabled = false;
        }
    }
}
