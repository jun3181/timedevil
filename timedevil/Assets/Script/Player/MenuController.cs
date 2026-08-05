// Assets/Script/Player/MenuController.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteAlways]
public class MenuController : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuUI;
    public TextMeshProUGUI[] menuItems;
    public TextMeshProUGUI panelText;

    [Header("Refs")]
    public GameManager manager;

    [Header("Debug")]
    [SerializeField] private bool debugMenu = true;

    [Header("Retro Menu View")]
    [SerializeField] private bool autoBuildRetroMenu = true;
    [SerializeField] private string menuFrameResourcePath = "my_asset/menu_window_frame";
    [SerializeField] private Vector2 windowOffsetFromTopLeft = new Vector2(80f, -50f);
    [SerializeField] private Vector2 windowSize = new Vector2(630f, 360f);
    [SerializeField] private Vector2 itemGridOrigin = new Vector2(72f, -90f);
    [SerializeField] private Vector2 itemGridSpacing = new Vector2(265f, 105f);
    [SerializeField] private Vector2 itemSize = new Vector2(190f, 58f);
    [SerializeField] private float itemFontSize = 42f;
    [SerializeField] private float cursorGapAfterText = 14f;
    [SerializeField] private string[] menuLabels = { "item", "card", "deck", "option", "close", "exit" };
    [SerializeField] private bool hidePanelTextInRetroView = true;
    [SerializeField] private bool preserveManualLayout = true;

    private int currentIndex = 0;
    private bool isPaused = false;
    private RectTransform retroContentRoot;
    private TextMeshProUGUI cursorText;
    private static Sprite generatedFrameSprite;
    private static readonly string[] DefaultMenuLabels = { "item", "card", "deck", "option", "close", "exit" };

    public bool IsOpen => isPaused;

    private void Awake()
    {
        if (Application.isPlaying && !manager) manager = GameManager.Instance;
        EnsureRetroMenuView();
    }

    private void OnEnable()
    {
        EnsureRetroMenuView();
        HighlightCurrent();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;

        UnityEditor.EditorApplication.delayCall -= RefreshRetroMenuViewInEditor;
        UnityEditor.EditorApplication.delayCall += RefreshRetroMenuViewInEditor;
#endif
    }

#if UNITY_EDITOR
    private void RefreshRetroMenuViewInEditor()
    {
        UnityEditor.EditorApplication.delayCall -= RefreshRetroMenuViewInEditor;

        if (this == null || Application.isPlaying) return;

        EnsureRetroMenuView();
        HighlightCurrent();
    }
#endif

    public void Open()
    {
        if (isPaused) return;

        if (debugMenu) Debug.Log("[MenuController] Open()", this);

        EnsureRetroMenuView();
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

        if (autoBuildRetroMenu)
            SetCurrentIndex(GetGridNeighbor(0, delta));
        else
            SetCurrentIndex((currentIndex + delta + menuItems.Length) % menuItems.Length);
    }

    public void NavigateHorizontal(int delta)
    {
        if (!isPaused || menuItems == null || menuItems.Length == 0) return;
        SetCurrentIndex(GetGridNeighbor(delta, 0));
    }

    public void NavigateVertical(int delta)
    {
        Navigate(delta);
    }

    public void SubmitCurrent()
    {
        if (!isPaused) return;

        if (debugMenu) Debug.Log($"[MenuController] Submit index={currentIndex}", this);

        switch (currentIndex)
        {
            case 0: // Item
                Debug.Log("[MenuController] Item selected (UI pending)", this);
                break;

            case 1: // Card
                Debug.Log("[MenuController] Card selected (UI pending)", this);
                break;

            case 2: // Deck
                Debug.Log("[MenuController] Deck selected (UI pending)", this);
                break;

            case 3: // Option
                Debug.Log("[MenuController] Option selected", this);
                break;

            case 4: // Close
                Close();
                break;

            case 5: // Exit
                Debug.Log("[MenuController] Exit selected", this);
                Application.Quit();
                break;
        }
    }

    private void SetCurrentIndex(int nextIndex)
    {
        if (menuItems == null || menuItems.Length == 0) return;

        nextIndex = (nextIndex + menuItems.Length) % menuItems.Length;
        if (nextIndex == currentIndex) return;

        currentIndex = nextIndex;
        if (debugMenu) Debug.Log($"[MenuController] Navigate -> {currentIndex}", this);

        HighlightCurrent();
    }

    private int GetGridNeighbor(int horizontalDelta, int verticalDelta)
    {
        if (menuItems == null || menuItems.Length == 0) return currentIndex;
        if (horizontalDelta == 0 && verticalDelta == 0) return currentIndex;

        int rowCount = GetGridRowCount();

        if (autoBuildRetroMenu && menuItems.Length > 1)
        {
            if (horizontalDelta != 0)
            {
                int column = GetGridColumn(currentIndex);
                int row = GetGridRow(currentIndex);
                int nextColumn = column == 0 ? 1 : 0;
                int nextIndex = nextColumn * rowCount + row;
                return nextIndex < menuItems.Length ? nextIndex : currentIndex;
            }

            if (verticalDelta != 0)
            {
                int column = GetGridColumn(currentIndex);
                int row = GetGridRow(currentIndex);
                int direction = verticalDelta > 0 ? 1 : -1;

                for (int i = 0; i < rowCount; i++)
                {
                    row = (row + direction + rowCount) % rowCount;
                    int nextIndex = column * rowCount + row;
                    if (nextIndex < menuItems.Length)
                        return nextIndex;
                }

                return currentIndex;
            }
        }

        int step = horizontalDelta != 0 ? (horizontalDelta > 0 ? 1 : -1) : (verticalDelta > 0 ? 2 : -2);
        return (currentIndex + step + menuItems.Length) % menuItems.Length;
    }

    private void HighlightCurrent()
    {
        if (menuItems != null)
        {
            for (int i = 0; i < menuItems.Length; i++)
                menuItems[i].color = autoBuildRetroMenu ? Color.white : (i == currentIndex ? Color.blue : Color.white);
        }

        MoveCursorToCurrentItem();

        if (panelText == null) return;

        if (autoBuildRetroMenu && hidePanelTextInRetroView)
        {
            panelText.gameObject.SetActive(false);
            return;
        }

        panelText.gameObject.SetActive(true);

        switch (currentIndex)
        {
            case 0: panelText.text = "open item"; break;
            case 1: panelText.text = "open card"; break;
            case 2: panelText.text = "open deck"; break;
            case 3: panelText.text = "open option"; break;
            case 4: panelText.text = "close menu"; break;
            case 5: panelText.text = "game exit"; break;
        }
    }

    private void EnsureRetroMenuView()
    {
        if (!autoBuildRetroMenu || menuUI == null) return;

        RectTransform menuRoot = menuUI.transform as RectTransform;
        if (menuRoot == null) return;

        bool frameAlreadyExists = menuRoot.Find("MenuWindowFrame") != null;
        RectTransform frame = GetOrCreateRect(menuRoot, "MenuWindowFrame");
        if (!preserveManualLayout || !frameAlreadyExists)
        {
            frame.anchorMin = new Vector2(0f, 1f);
            frame.anchorMax = new Vector2(0f, 1f);
            frame.pivot = new Vector2(0f, 1f);
            frame.anchoredPosition = windowOffsetFromTopLeft;
            frame.sizeDelta = windowSize;
            frame.localScale = Vector3.one;
        }
        frame.SetAsFirstSibling();

        Image frameImage = frame.GetComponent<Image>();
        if (frameImage == null) frameImage = frame.gameObject.AddComponent<Image>();
        frameImage.sprite = LoadMenuFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        bool contentAlreadyExists = menuRoot.Find("MenuContent") != null;
        retroContentRoot = GetOrCreateRect(menuRoot, "MenuContent");
        if (!preserveManualLayout || !contentAlreadyExists)
        {
            retroContentRoot.anchorMin = new Vector2(0f, 1f);
            retroContentRoot.anchorMax = new Vector2(0f, 1f);
            retroContentRoot.pivot = new Vector2(0f, 1f);
            retroContentRoot.anchoredPosition = windowOffsetFromTopLeft;
            retroContentRoot.sizeDelta = windowSize;
            retroContentRoot.localScale = Vector3.one;
        }
        retroContentRoot.SetAsLastSibling();

        EnsureRetroMenuItems();
        LayoutMenuItems();
        EnsureCursor();
        MoveCursorToCurrentItem();
    }

    private RectTransform GetOrCreateRect(RectTransform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null && existing is RectTransform existingRect)
            return existingRect;

        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void LayoutMenuItems()
    {
        if (menuItems == null || retroContentRoot == null) return;

        for (int i = 0; i < menuItems.Length; i++)
        {
            TextMeshProUGUI itemText = menuItems[i];
            if (itemText == null) continue;

            RectTransform itemRoot = GetItemRoot(itemText);
            bool itemAlreadyInContent = itemRoot.parent == retroContentRoot;
            if (itemRoot.parent != retroContentRoot)
                itemRoot.SetParent(retroContentRoot, false);

            if (!preserveManualLayout || !itemAlreadyInContent)
            {
                itemRoot.anchorMin = new Vector2(0f, 1f);
                itemRoot.anchorMax = new Vector2(0f, 1f);
                itemRoot.pivot = new Vector2(0f, 1f);
                itemRoot.anchoredPosition = GetItemPosition(i);
                itemRoot.sizeDelta = itemSize;
                itemRoot.localScale = Vector3.one;
            }

            foreach (Image image in itemRoot.GetComponentsInChildren<Image>(true))
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = false;
            }

            StyleMenuItemText(itemText, i, itemRoot == itemText.rectTransform, !preserveManualLayout || !itemAlreadyInContent);
        }
    }

    private RectTransform GetItemRoot(TextMeshProUGUI itemText)
    {
        RectTransform textRect = itemText.rectTransform;
        Transform parent = textRect.parent;

        if (parent != null &&
            parent != menuUI.transform &&
            parent != retroContentRoot &&
            parent is RectTransform parentRect &&
            parent.GetComponent<Image>() != null)
        {
            return parentRect;
        }

        return textRect;
    }

    private void StyleMenuItemText(TextMeshProUGUI text, int index, bool textIsItemRoot, bool applyDefaultTextTransform)
    {
        text.text = GetMenuLabel(index, text.text);
        text.color = Color.white;
        text.fontSize = itemFontSize;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = false;
        text.raycastTarget = false;
        text.characterSpacing = 0f;

        if (textIsItemRoot || !applyDefaultTextTransform)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = itemSize;
        rect.localScale = Vector3.one;
    }

    private string GetMenuLabel(int index, string fallback)
    {
        if (menuLabels != null &&
            menuLabels.Length >= DefaultMenuLabels.Length &&
            index >= 0 &&
            index < menuLabels.Length &&
            !string.IsNullOrEmpty(menuLabels[index]))
        {
            return menuLabels[index];
        }

        if (index >= 0 && index < DefaultMenuLabels.Length)
            return DefaultMenuLabels[index];

        return fallback;
    }

    private void EnsureRetroMenuItems()
    {
        if (retroContentRoot == null) return;

        int serializedCount = menuLabels != null ? menuLabels.Length : 0;
        int targetCount = Mathf.Max(DefaultMenuLabels.Length, serializedCount);
        if (menuItems != null && menuItems.Length == targetCount) return;

        TextMeshProUGUI[] existing = menuItems ?? new TextMeshProUGUI[0];
        TextMeshProUGUI fontSource = GetFirstMenuText();
        TextMeshProUGUI[] expanded = new TextMeshProUGUI[targetCount];

        if (targetCount == 6 && existing.Length == 4)
        {
            expanded[0] = existing[0];
            expanded[1] = existing[1];
            expanded[3] = existing[2];
            expanded[5] = existing[3];
        }
        else
        {
            int copyCount = Mathf.Min(existing.Length, expanded.Length);
            for (int i = 0; i < copyCount; i++)
                expanded[i] = existing[i];
        }

        for (int i = 0; i < expanded.Length; i++)
        {
            if (expanded[i] == null)
                expanded[i] = CreateMenuItemText(i, fontSource);
        }

        menuItems = expanded;
        currentIndex = Mathf.Clamp(currentIndex, 0, menuItems.Length - 1);
    }

    private TextMeshProUGUI CreateMenuItemText(int index, TextMeshProUGUI fontSource)
    {
        GameObject item = new GameObject($"{GetMenuLabel(index, $"menu_{index}")}_text", typeof(RectTransform), typeof(TextMeshProUGUI));
        item.layer = retroContentRoot.gameObject.layer;
        item.transform.SetParent(retroContentRoot, false);

        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        if (fontSource != null)
            text.font = fontSource.font;

        return text;
    }

    private void EnsureCursor()
    {
        if (retroContentRoot == null) return;

        Transform existing = retroContentRoot.Find("MenuCursor");
        if (existing != null)
        {
            cursorText = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject cursor = new GameObject("MenuCursor", typeof(RectTransform), typeof(TextMeshProUGUI));
            cursor.layer = retroContentRoot.gameObject.layer;
            cursor.transform.SetParent(retroContentRoot, false);
            cursorText = cursor.GetComponent<TextMeshProUGUI>();
        }

        TextMeshProUGUI fontSource = GetFirstMenuText();
        if (fontSource != null)
            cursorText.font = fontSource.font;

        cursorText.color = Color.white;
        cursorText.fontSize = itemFontSize;
        cursorText.fontStyle = FontStyles.Bold;
        cursorText.alignment = TextAlignmentOptions.Center;
        cursorText.enableWordWrapping = false;
        cursorText.overflowMode = TextOverflowModes.Overflow;
        cursorText.richText = false;
        cursorText.raycastTarget = false;

        RectTransform cursorRect = cursorText.rectTransform;
        cursorRect.anchorMin = new Vector2(0f, 1f);
        cursorRect.anchorMax = new Vector2(0f, 1f);
        cursorRect.pivot = new Vector2(0f, 1f);
        cursorRect.sizeDelta = new Vector2(46f, itemSize.y);
        cursorRect.localScale = Vector3.one;
        cursorText.transform.SetAsLastSibling();
    }

    private TextMeshProUGUI GetFirstMenuText()
    {
        if (menuItems == null) return null;

        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] != null) return menuItems[i];
        }

        return null;
    }

    private void MoveCursorToCurrentItem()
    {
        if (!autoBuildRetroMenu || cursorText == null || menuItems == null || menuItems.Length == 0) return;

        TextMeshProUGUI itemText = currentIndex >= 0 && currentIndex < menuItems.Length ? menuItems[currentIndex] : null;
        RectTransform itemRoot = itemText != null ? GetItemRoot(itemText) : null;
        Vector2 itemPosition = itemRoot != null ? itemRoot.anchoredPosition : GetItemPosition(currentIndex);
        if (itemText != null && itemText.rectTransform != itemRoot)
            itemPosition += itemText.rectTransform.anchoredPosition;

        float textWidth = itemText != null ? itemText.GetPreferredValues(itemText.text).x : itemSize.x;

        cursorText.text = "<";
        cursorText.rectTransform.anchoredPosition = new Vector2(
            itemPosition.x + textWidth + cursorGapAfterText,
            itemPosition.y + 1f
        );
    }

    private Vector2 GetItemPosition(int index)
    {
        int column = GetGridColumn(index);
        int row = GetGridRow(index);

        return new Vector2(
            itemGridOrigin.x + column * itemGridSpacing.x,
            itemGridOrigin.y - row * itemGridSpacing.y
        );
    }

    private int GetGridColumn(int index)
    {
        return index / GetGridRowCount();
    }

    private int GetGridRow(int index)
    {
        return index % GetGridRowCount();
    }

    private int GetGridRowCount()
    {
        int count = menuItems != null && menuItems.Length > 0
            ? menuItems.Length
            : Mathf.Max(DefaultMenuLabels.Length, menuLabels != null ? menuLabels.Length : 0);

        return Mathf.Max(1, Mathf.CeilToInt(count / 2f));
    }

    private Sprite LoadMenuFrameSprite()
    {
        Sprite loaded = Resources.Load<Sprite>(menuFrameResourcePath);
        if (loaded != null) return loaded;

        if (generatedFrameSprite != null) return generatedFrameSprite;

        const int width = 256;
        const int height = 144;
        const int border = 6;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color32 black = new Color32(0, 0, 0, 255);
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool edge = x < border || x >= width - border || y < border || y >= height - border;
                texture.SetPixel(x, y, edge ? white : black);
            }
        }

        texture.Apply(false, true);
        generatedFrameSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border)
        );

        return generatedFrameSprite;
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
        
        if(CameraManager.Instance!=null) {
            CameraModeId currentCameraMode = CameraManager.Instance.CurrentMode;
            PlayerReturnContext.ReturnCameraMode = currentCameraMode;
            PlayerReturnContext.RestoreCameraStatePending = true;

            CameraManager.Instance.TryGetSnapshot(out CameraModeId camMode, out float camOrtho, out Vector3 fixedPos, out string boundsName);
            PlayerReturnContext.ReturnCameraBoundsName = boundsName;
            if(currentCameraMode==CameraModeId.Fixed || currentCameraMode==CameraModeId.Cutscene) {
                PlayerReturnContext.ReturnCameraFixedPos = fixedPos;
            }
        }
    }
}
