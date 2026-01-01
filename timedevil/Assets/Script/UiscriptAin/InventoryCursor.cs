using UnityEngine;

public class InventoryCursor : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private RectTransform highlight;

    [Header("행 설정")]
    [SerializeField] private int rowCount = 6;
    [SerializeField] private float rowHeight = 60f;
    [SerializeField] private Vector2 topAnchoredPos = Vector2.zero;

    [Header("상태")]
    [SerializeField] private int currentIndex = 0;

    // 🔥 InventoryDisplay에서 현재 선택된 슬롯 번호를 가져가기 위해 추가한 프로퍼티
    public int CurrentIndex => currentIndex;

    private void Reset()
    {
        if (!highlight) highlight = GetComponent<RectTransform>();
    }

    private void Start()
    {
        SetToIndex(currentIndex);
    }

    private void Update()
    {
        // 🔥 설명창 열려 있으면 커서 이동 입력 무시
        if (InventoryDisplay.IsAnyDescriptionOpen)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) Move(-1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) Move(+1);
    }

    private void Move(int delta)
    {
        int next = Mathf.Clamp(currentIndex + delta, 0, rowCount - 1);
        if (next == currentIndex) return;
        currentIndex = next;
        SetToIndex(currentIndex);
    }

    private void SetToIndex(int index)
    {
        Vector2 pos = topAnchoredPos + new Vector2(0f, -rowHeight * index);
        highlight.anchoredPosition = pos;
    }

    // ★ 페이지가 바뀔 때 호출: 맨 윗칸으로 이동
    public void ResetToTop()
    {
        currentIndex = 0;
        SetToIndex(currentIndex);
    }
}
