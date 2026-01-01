using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // ✅ 싱글톤 (전역 접근 가능)
    public static GameManager Instance { get; private set; }

    [Header("Dialogue UI")]
    public TextMeshProUGUI talkText;
    public GameObject talkPanel;

    private GameObject scanObject;
    public bool isAction;

    void Awake()
    {
        // 싱글톤 유지 + 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않음
    }

    /// <summary>
    /// 오브젝트와 상호작용 (대화/아이템 획득 등)
    /// </summary>
    public void Action(GameObject scanObj)
    {
        if (scanObj == null || talkText == null || talkPanel == null)
        {
            Debug.LogWarning("[GameManager] UI or ScanObj missing");
            return;
        }

        if (isAction)
        {
            // 액션 종료
            isAction = false;
            talkPanel.SetActive(false);
        }
        else
        {
            // 액션 시작
            isAction = true;
            scanObject = scanObj;
            talkText.text = $"{scanObj.name} 과(와) 상호작용!";
            talkPanel.SetActive(true);
        }
    }
}
