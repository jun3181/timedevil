// Assets/Script/Player/MenuController.cs
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuUI;
    public TextMeshProUGUI[] menuItems;
    public TextMeshProUGUI panelText;

    [Header("Refs")]
    public GameManager manager;

    [Header("Scene Transition")]
    [SerializeField] private bool useFaderIfExists = true;

    [Header("Debug")]
    [SerializeField] private bool debugMenu = true;

    private int currentIndex = 0;
    private bool isPaused = false;

    public bool IsOpen => isPaused;

    private void Awake()
    {
        if (!manager) manager = GameManager.Instance;
    }

    private void OnEnable()
    {
        HighlightCurrent();
    }

    public void Open()
    {
        if (isPaused) return;

        if (debugMenu) Debug.Log("[MenuController] Open()", this);

        if (menuUI) menuUI.SetActive(true);
        isPaused = true;

        if (manager != null) manager.LockAction();

        Time.timeScale = 0f;
        HighlightCurrent();
    }

    public void Close()
    {
        if (!isPaused) return;

        if (debugMenu) Debug.Log("[MenuController] Close()", this);

        if (menuUI) menuUI.SetActive(false);
        isPaused = false;

        if (manager != null) manager.UnlockAction();

        Time.timeScale = 1f;
    }

    public void Navigate(int delta)
    {
        if (!isPaused || menuItems == null || menuItems.Length == 0) return;

        currentIndex = (currentIndex + delta + menuItems.Length) % menuItems.Length;
        if (debugMenu) Debug.Log($"[MenuController] Navigate -> {currentIndex}", this);

        HighlightCurrent();
    }

    public void SubmitCurrent()
    {
        if (!isPaused) return;

        string current = SceneManager.GetActiveScene().name;
        if (debugMenu) Debug.Log($"[MenuController] Submit index={currentIndex}", this);

        switch (currentIndex)
        {
            case 0: // Inventory
                CacheReturnPoint(current);
                Close();
                SceneLoader.Load("InventoryScene", useFaderIfExists);
                break;

            case 1: // Card
                CacheReturnPoint(current);
                Close();
                SceneLoader.Load("Card", useFaderIfExists);
                break;

            case 2: // Option
                Debug.Log("[MenuController] Option selected", this);
                break;

            case 3: // Exit
                Debug.Log("[MenuController] Exit selected", this);
                Application.Quit();
                break;
        }
    }

    private void HighlightCurrent()
    {
        if (menuItems != null)
        {
            for (int i = 0; i < menuItems.Length; i++)
                menuItems[i].color = (i == currentIndex) ? Color.blue : Color.white;
        }

        if (panelText == null) return;

        switch (currentIndex)
        {
            case 0: panelText.text = "open inventory"; break;
            case 1: panelText.text = "manage deck"; break;
            case 2: panelText.text = "open option"; break;
            case 3: panelText.text = "game exit"; break;
        }
    }

    private void CacheReturnPoint(string currentScene)
    {
        var playerMove = FindObjectOfType<PlayerMove>(true);
        if (playerMove)
        {
            PlayerReturnContext.ReturnPosition = (Vector2)playerMove.transform.position;
            PlayerReturnContext.HasReturnPosition = true;
        }

        PlayerReturnContext.ReturnSceneName = currentScene;
        PlayerReturnContext.CameraRebindRequested = true;
    }
}
