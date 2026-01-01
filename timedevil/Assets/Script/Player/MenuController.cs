using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject menuUI;
    public GameManager manager;

    public TextMeshProUGUI[] menuItems;
    public TextMeshProUGUI panelText;

    int currentIndex = 0;
    bool isPaused = false;

    void OnEnable()
    {
        HighlightCurrent();
    }

    void Update()
    {
        // Q: 토글 열기/닫기
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isPaused) Resume();
            else Pause();
        }

        if (!menuUI.activeSelf) return;

        // 항목 이동
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + menuItems.Length) % menuItems.Length;
            HighlightCurrent();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % menuItems.Length;
            HighlightCurrent();
        }

        // 실행
        if (Input.GetKeyDown(KeyCode.E))
        {
            string current = SceneManager.GetActiveScene().name;

            switch (currentIndex)
            {
                case 0: // Inventory
                    Debug.Log("Inventory selected → load InventoryScene");

                    // ★★★ 귀환 정보 저장 (좌표 + 씬 + 카메라 재바인딩 요청)
                    CacheReturnPoint(current);

                    // 시간/UI 복구 후 전환
                    Resume();
                    if (SceneFader.instance) SceneFader.instance.LoadSceneWithFade("InventoryScene");
                    else SceneManager.LoadScene("InventoryScene");
                    break;

                case 1: // Card
                    Debug.Log("Card selected");

                    // ★★★ 카드 화면도 동일하게 귀환 정보 저장
                    CacheReturnPoint(current);

                    Resume();
                    if (SceneFader.instance) SceneFader.instance.LoadSceneWithFade("Card");
                    else SceneManager.LoadScene("Card");
                    break;

                case 2: // Option
                    Debug.Log("Option selected");
                    break;

                case 3: // Exit
                    Debug.Log("Exit selected");
                    Application.Quit();
                    break;
            }
        }
    }

    // 현재 선택 하이라이트/설명
    void HighlightCurrent()
    {
        for (int i = 0; i < menuItems.Length; i++)
            menuItems[i].color = (i == currentIndex) ? Color.blue : Color.white;

        switch (currentIndex)
        {
            case 0: panelText.text = "open inventory"; break;
            case 1: panelText.text = "manage deck"; break;
            case 2: panelText.text = "open option"; break;
            case 3: panelText.text = "game exit"; break;
        }
    }

    void Pause()
    {
        if (menuUI) menuUI.SetActive(true);
        isPaused = true;
        if (manager != null) manager.isAction = true;
        Time.timeScale = 0f;
        HighlightCurrent();
    }

    void Resume()
    {
        if (menuUI) menuUI.SetActive(false);
        isPaused = false;
        if (manager != null) manager.isAction = false;
        Time.timeScale = 1f;
    }

    // ★★★ 현재 플레이어 좌표와 귀환 관련 플래그를 저장
    // ★★★ 현재 플레이어 좌표와 귀환 관련 플래그 + 몬스터 스냅샷 저장
    private void CacheReturnPoint(string currentScene)
    {
        // 1) 플레이어 위치 저장
        var player = FindObjectOfType<PlayerAction>();
        if (player)
        {
            PlayerReturnContext.ReturnPosition = (Vector2)player.transform.position;
            PlayerReturnContext.HasReturnPosition = true;
        }

        // 2) 귀환 씬 이름
        PlayerReturnContext.ReturnSceneName = currentScene;

        // 3) 카메라 재바인딩 플래그
        PlayerReturnContext.CameraRebindRequested = true;
        // PlayerReturnContext.TargetVcamName = "CM vcam1"; // 필요시

        // 4) ★★★ 몬스터 스냅샷 저장(가장 가까운/활성 적 1기)
        var enemy = FindNearestEnemyTo(player ? player.transform.position : (Vector3?)null);
        if (enemy != null)
        {
            // (1) 고유 ID
            var id = enemy.GetComponent<EnemyInstanceId>();
            PlayerReturnContext.MonsterInstanceId = id ? id.Id : null;

            // (2) 이름 폴백
            PlayerReturnContext.MonsterNameInScene = enemy.name;

            // (3) 좌표 저장(최후 폴백)
            PlayerReturnContext.MonsterReturnPosition = (Vector2)enemy.transform.position;

            // (4) 스냅샷 서비스가 있으면 현재 상태 캡처
            if (WorldNPCStateService.Instance != null)
            {
                // id가 있으면 id로, 없으면 name으로 저장
                string key = !string.IsNullOrEmpty(PlayerReturnContext.MonsterInstanceId)
                             ? PlayerReturnContext.MonsterInstanceId
                             : PlayerReturnContext.MonsterNameInScene;

                WorldNPCStateService.Instance.SaveSnapshot(enemy);
                // ↑ 내부 구현이 TryGetSnapshot/ApplyTo와 호환되도록 되어 있어야 합니다.
            }
        }
        else
        {
            // 아무 적도 못 찾았으면 이전 잔재가 적용되지 않게 모두 정리
            PlayerReturnContext.MonsterInstanceId = null;
            PlayerReturnContext.MonsterNameInScene = null;
            PlayerReturnContext.MonsterReturnPosition = default;
        }
    }

    // ★ 씬 내에서 "적" 후보를 찾는 헬퍼
    private GameObject FindNearestEnemyTo(Vector3? originOpt)
    {
        Vector3 origin = originOpt ?? Vector3.zero;

        // 1) EnemyInstanceId 달린 적 우선
        var withId = FindObjectsOfType<EnemyInstanceId>(true);
        GameObject best = null; float bestD = float.PositiveInfinity;
        foreach (var e in withId)
        {
            if (!e || !e.gameObject.activeInHierarchy) continue;
            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < bestD) { bestD = d; best = e.gameObject; }
        }
        if (best) return best;

        // 2) 태그 기반 폴백
        var tagged = GameObject.FindGameObjectsWithTag("Enemy");
        best = null; bestD = float.PositiveInfinity;
        foreach (var go in tagged)
        {
            if (!go || !go.activeInHierarchy) continue;
            float d = (go.transform.position - origin).sqrMagnitude;
            if (d < bestD) { bestD = d; best = go; }
        }
        if (best) return best;

        // 3) 움직이는 AI 컴포넌트 폴백(필요 시 타입 추가)
        var movers = FindObjectsOfType<UndeadMover>(true);
        best = null; bestD = float.PositiveInfinity;
        foreach (var m in movers)
        {
            if (!m || !m.gameObject.activeInHierarchy) continue;
            float d = (m.transform.position - origin).sqrMagnitude;
            if (d < bestD) { bestD = d; best = m.gameObject; }
        }
        return best;
    }

}
