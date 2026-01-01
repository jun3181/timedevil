using UnityEngine;
using UnityEngine.UI; // Text 사용 시
// using TMPro; // 만약 TMP_Text를 쓴다면 주석 해제

public class InventoryPageManagerKeys : MonoBehaviour
{
    [Header("페이지 오브젝트 (하나만 활성화)")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;

    [Header("페이지 텍스트(선택)")]
    [SerializeField] private Text pageText; // TMP라면 TMP_Text로 바꾸세요
    // [SerializeField] private TMP_Text pageText;

    [Header("커서 컨트롤러")]
    [SerializeField] private InventoryCursor cursor;

    private int currentPage = 1;   // 1 또는 2
    private const int totalPages = 2;

    private void Start()
    {
        ApplyPage(currentPage, resetCursor: true);
    }

    private void Update()
    {
        // 🔥 설명창 열려 있으면 페이지 전환 입력 무시
        if (InventoryDisplay.IsAnyDescriptionOpen)
            return;

        // → 다음 페이지
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                ApplyPage(currentPage, resetCursor: true);
                Debug.Log($"{currentPage} 페이지로 이동");
            }
        }

        // ← 이전 페이지
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentPage > 1)
            {
                currentPage--;
                ApplyPage(currentPage, resetCursor: true);
                Debug.Log($"{currentPage} 페이지로 이동");
            }
        }
    }

    private void ApplyPage(int page, bool resetCursor)
    {
        if (page1) page1.SetActive(page == 1);
        if (page2) page2.SetActive(page == 2);

        if (pageText) pageText.text = $"{page} / {totalPages}";

        if (resetCursor && cursor != null)
            cursor.ResetToTop();
    }
}
