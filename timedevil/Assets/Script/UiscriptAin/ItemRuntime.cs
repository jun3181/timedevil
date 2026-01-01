using UnityEngine;

/// <summary>
/// 다른 씬에서도 살아남는 인벤토리 런타임 매니저
/// - 씬을 옮겨도 인벤토리 유지
/// - JSON으로 세이브/로드
/// - 다른 스크립트는 ItemRuntime.Instance 로 접근
/// </summary>
public class ItemRuntime : MonoBehaviour
{
    public static ItemRuntime Instance { get; private set; }

    [Header("초기 인벤토리 JSON (Resources 폴더 기준 이름)")]
    [Tooltip("세이브 파일이 없을 때 사용할 기본 JSON (예: items)")]
    public string defaultJsonName = "items";

    [Header("현재 인벤토리 데이터 (런타임 상태)")]
    [SerializeField] private InventorySaveData currentData;   // 🔥 필드라서 Header OK
    public InventorySaveData CurrentData                     // 코드에서 쓸 프로퍼티
    {
        get => currentData;
        private set => currentData = value;
    }

    private const string SaveFileName = "items_save.json";

    private void Awake()
    {
        // 싱글턴 패턴 + 씬 전환 시 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 세이브 파일이 있으면 거기서 로드, 없으면 기본 JSON에서 로드
        if (ItemSaveStore.HasSave(SaveFileName))
        {
            LoadFromDisk();
        }
        else
        {
            LoadFromDefaultJson();
        }
    }

    /// <summary>
    /// Resources/{defaultJsonName}.json 에서 초기 인벤토리 로드 (새 게임 시작 느낌)
    /// </summary>
    public void LoadFromDefaultJson()
    {
        if (string.IsNullOrEmpty(defaultJsonName))
        {
            Debug.LogError("❌ defaultJsonName 이 비어 있습니다.");
            return;
        }

        TextAsset json = Resources.Load<TextAsset>(defaultJsonName);
        if (json == null)
        {
            Debug.LogError($"❌ Resources/{defaultJsonName}.json 을 찾을 수 없습니다.");
            return;
        }

        CurrentData = JsonUtility.FromJson<InventorySaveData>(json.text);
        if (CurrentData == null || CurrentData.items == null)
        {
            Debug.LogError("⚠️ 초기 JSON 파싱 실패 또는 items 배열이 비어 있습니다.");
            CurrentData = new InventorySaveData { items = new InventoryItemEntry[0] };
            return;
        }

        Debug.Log($"✅ 기본 JSON에서 {CurrentData.items.Length}개의 인벤토리 아이템을 로드했습니다.");
    }

    /// <summary>
    /// 디스크에 저장된 JSON 세이브에서 인벤토리 로드
    /// </summary>
    public void LoadFromDisk()
    {
        ItemSave save = ItemSaveStore.Load(SaveFileName);
        if (save == null || save.items == null)
        {
            Debug.LogWarning("⚠️ 세이브 파일이 없거나 파싱 실패. 기본 JSON을 사용합니다.");
            LoadFromDefaultJson();
            return;
        }

        CurrentData = new InventorySaveData
        {
            items = save.items
        };

        Debug.Log($"✅ 세이브 파일에서 {CurrentData.items.Length}개의 인벤토리 아이템을 로드했습니다.");
    }

    /// <summary>
    /// 현재 인벤토리 상태를 디스크(JSON)로 저장
    /// </summary>
    public void SaveToDisk()
    {
        if (CurrentData == null || CurrentData.items == null)
        {
            Debug.LogWarning("⚠️ 저장할 인벤토리 데이터가 없습니다.");
            return;
        }

        ItemSave save = new ItemSave
        {
            items = CurrentData.items
        };

        ItemSaveStore.Save(save, SaveFileName);
        Debug.Log($"💾 인벤토리 세이브 완료: {SaveFileName}");
    }

    // ================== 편의 메서드들 (원하면 자유롭게 추가) ==================

    /// <summary>
    /// 특정 id 아이템의 현재 수량 조회 (없으면 0)
    /// </summary>
    public int GetQuantity(string id)
    {
        if (CurrentData == null || CurrentData.items == null) return 0;

        foreach (var e in CurrentData.items)
        {
            if (e != null && e.id == id)
                return e.quantity;
        }
        return 0;
    }

    /// <summary>
    /// 특정 id 아이템의 수량을 delta 만큼 변경 (마이너스도 가능)
    /// </summary>
    public void AddQuantity(string id, int delta)
    {
        if (CurrentData == null)
        {
            CurrentData = new InventorySaveData { items = new InventoryItemEntry[0] };
        }

        // 1) 이미 있는지 찾기
        for (int i = 0; i < CurrentData.items.Length; i++)
        {
            var e = CurrentData.items[i];
            if (e != null && e.id == id)
            {
                e.quantity += delta;
                if (e.quantity < 0) e.quantity = 0;
                return;
            }
        }

        // 2) 없으면 새로 추가 (delta > 0 인 경우만)
        if (delta > 0)
        {
            var list = new System.Collections.Generic.List<InventoryItemEntry>(CurrentData.items);
            list.Add(new InventoryItemEntry
            {
                id = id,
                quantity = delta
            });
            CurrentData.items = list.ToArray();
        }
    }
}
