// Assets/Script/Player/MenuController.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteAlways]
public class MenuController : MonoBehaviour
{
    private enum MenuFocusMode
    {
        Main,
        Item,
        Card,
        Deck,
        Status,
        ExitConfirm
    }

    private enum MenuPanelFocusArea
    {
        List,
        Close,
        Page
    }

    private struct CardMenuEntry
    {
        public string id;
        public BaseCardSO card;
        public string label;
    }

    private struct ItemMenuEntry
    {
        public string id;
        public ItemSO item;
        public string label;
    }

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
    [SerializeField] private Vector2 contentOffsetFromTopLeft = new Vector2(80f, -1f);
    [SerializeField] private Vector2 windowSize = new Vector2(630f, 360f);
    [SerializeField] private Vector2 itemGridOrigin = new Vector2(72f, -90f);
    [SerializeField] private Vector2 itemGridSpacing = new Vector2(265f, 105f);
    [SerializeField] private Vector2 itemSize = new Vector2(190f, 58f);
    [SerializeField] private float itemFontSize = 42f;
    [SerializeField] private float cursorGapAfterText = 14f;
    [SerializeField] private string[] menuLabels = { "item", "card", "deck", "option", "status", "exit" };
    [SerializeField] private bool hidePanelTextInRetroView = true;
    [SerializeField] private bool preserveManualLayout = true;
    [SerializeField] private bool repairLegacySubWindowLayout = true;

    [Header("Item Window View")]
    [SerializeField] private bool autoBuildItemWindow = true;
    [SerializeField] private bool previewItemWindowInEditor = false;
    [SerializeField] private bool preserveManualItemLayout = true;
    [SerializeField] private ItemDatabaseSO itemDatabase;
    [SerializeField] private int itemEntriesPerPage = 4;
    [SerializeField] private bool useDefaultInventoryJsonWhenRuntimeMissing = true;
    [SerializeField] private string defaultInventoryJsonName = "items";
    [SerializeField] private Vector2 itemWindowOffsetFromTopLeft = new Vector2(690f, -50f);
    [SerializeField] private Vector2 itemWindowSize = new Vector2(600f, 300f);
    [SerializeField] private Vector2 itemWindowListOrigin = new Vector2(44f, -52f);
    [SerializeField] private Vector2 itemWindowEntrySize = new Vector2(340f, 50f);
    [SerializeField] private float itemWindowRowSpacing = 60f;
    [SerializeField] private Vector2 itemWindowClosePosition = new Vector2(380f, -52f);
    [SerializeField] private Vector2 itemWindowCloseSize = new Vector2(170f, 50f);
    [SerializeField] private Vector2 itemWindowPagePosition = new Vector2(390f, -225f);
    [SerializeField] private Vector2 itemWindowPageSize = new Vector2(120f, 50f);
    [SerializeField] private Vector2 itemInfoWindowOffsetFromTopLeft = new Vector2(1320f, -50f);
    [SerializeField] private Vector2 itemInfoWindowSize = new Vector2(340f, 300f);
    [SerializeField] private string itemCloseLabel = "close";
    [SerializeField] private string emptyItemLabel = "����";
    [SerializeField] private string[] itemPageOneLabels = { "������ �̸� x 2", "������ �̸� x 3", "������ �̸� x 1", "������ �̸�" };
    [SerializeField] private string[] itemPageTwoLabels = { "������ �̸� x 4", "������ �̸� x 5", "������ �̸� x 6", "������ �̸�" };

    [Header("Card Window View")]
    [SerializeField] private bool autoBuildCardWindow = true;
    [SerializeField] private bool previewCardWindowInEditor = false;
    [SerializeField] private bool preserveManualCardLayout = true;
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private int cardEntriesPerPage = 4;
    [SerializeField] private Vector2 cardWindowOffsetFromTopLeft = new Vector2(690f, -50f);
    [SerializeField] private Vector2 cardWindowSize = new Vector2(600f, 300f);
    [SerializeField] private Vector2 cardWindowListOrigin = new Vector2(44f, -52f);
    [SerializeField] private Vector2 cardWindowEntrySize = new Vector2(340f, 50f);
    [SerializeField] private float cardWindowRowSpacing = 60f;
    [SerializeField] private Vector2 cardWindowClosePosition = new Vector2(380f, -52f);
    [SerializeField] private Vector2 cardWindowCloseSize = new Vector2(170f, 50f);
    [SerializeField] private Vector2 cardWindowPagePosition = new Vector2(390f, -225f);
    [SerializeField] private Vector2 cardWindowPageSize = new Vector2(120f, 50f);
    [SerializeField] private Vector2 cardPreviewOffsetFromTopLeft = new Vector2(1320f, -50f);
    [SerializeField] private Vector2 cardPreviewSize = new Vector2(170f, 240f);
    [SerializeField] private string cardCloseLabel = "close";
    [SerializeField] private string emptyCardLabel = "����";

    [Header("Card Preview Text")]
    [SerializeField] private bool overrideCardPreviewTextSize = true;
    [SerializeField, Min(1f)] private float cardPreviewCornerFontSize = 18f;
    [SerializeField, Min(1f)] private float cardPreviewEffectFontSize = 13f;

    [Header("Deck Window View")]
    [SerializeField] private bool autoBuildDeckWindow = true;
    [SerializeField] private bool previewDeckWindowInEditor = false;
    [SerializeField] private bool preserveManualDeckLayout = true;
    [SerializeField] private string deckCloseLabel = "close";
    [SerializeField] private string emptyDeckLabel = "����";

    [Header("Status Window View")]
    [SerializeField] private bool autoBuildStatusWindow = true;
    [SerializeField] private bool previewStatusWindowInEditor = false;
    [SerializeField] private bool preserveManualStatusLayout = true;
    [SerializeField] private Vector2 statusWindowOffsetFromTopLeft = new Vector2(690f, -50f);
    [SerializeField] private Vector2 statusWindowSize = new Vector2(600f, 300f);
    [SerializeField] private Vector2 statusWindowListOrigin = new Vector2(44f, -52f);
    [SerializeField] private Vector2 statusWindowEntrySize = new Vector2(360f, 44f);
    [SerializeField] private float statusWindowRowSpacing = 48f;
    [SerializeField] private Vector2 statusWindowClosePosition = new Vector2(380f, -52f);
    [SerializeField] private Vector2 statusWindowCloseSize = new Vector2(170f, 50f);
    [SerializeField] private string statusCloseLabel = "close";
    [SerializeField] private string emptyStatusLabel = "no data";

    [Header("Exit Confirm View")]
    [SerializeField] private bool autoBuildExitConfirmWindow = true;
    [SerializeField] private bool previewExitConfirmInEditor = false;
    [SerializeField] private bool preserveManualExitConfirmLayout = true;
    [SerializeField] private Vector2 exitConfirmWindowOffsetFromTopLeft = new Vector2(690f, -50f);
    [SerializeField] private Vector2 exitConfirmWindowSize = new Vector2(820f, 360f);
    [SerializeField] private Vector2 exitConfirmMessagePosition = new Vector2(44f, -28f);
    [SerializeField] private Vector2 exitConfirmMessageSize = new Vector2(740f, 96f);
    [SerializeField] private Vector2 exitConfirmYesPosition = new Vector2(44f, -225f);
    [SerializeField] private Vector2 exitConfirmNoPosition = new Vector2(590f, -225f);
    [SerializeField] private Vector2 exitConfirmAnswerSize = new Vector2(210f, 60f);
    [SerializeField] private string exitConfirmMessage = DefaultExitConfirmMessage;
    [SerializeField] private string exitConfirmYesLabel = "예 : E";
    [SerializeField] private string exitConfirmNoLabel = DefaultExitConfirmNoLabel;

    [Header("Exit Behavior")]
    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;
    [SerializeField] private bool useFaderIfExists = true;

    private int currentIndex = 0;
    private bool isPaused = false;
    private MenuFocusMode focusMode = MenuFocusMode.Main;
    private MenuPanelFocusArea itemFocusArea = MenuPanelFocusArea.List;
    private MenuPanelFocusArea cardFocusArea = MenuPanelFocusArea.List;
    private MenuPanelFocusArea deckFocusArea = MenuPanelFocusArea.List;
    private bool itemWindowOpen = false;
    private bool cardWindowOpen = false;
    private bool deckWindowOpen = false;
    private bool statusWindowOpen = false;
    private int itemCurrentPage = 0;
    private int itemCurrentIndex = 0;
    private int cardCurrentPage = 0;
    private int cardCurrentIndex = 0;
    private int deckCurrentPage = 0;
    private int deckCurrentIndex = 0;
    private bool exitConfirmOpen = false;
    private RectTransform retroContentRoot;
    private TextMeshProUGUI cursorText;
    private RectTransform itemWindowFrame;
    private RectTransform itemWindowContentRoot;
    private TextMeshProUGUI[] itemWindowTexts;
    private TextMeshProUGUI itemWindowCloseText;
    private TextMeshProUGUI itemWindowPageText;
    private TextMeshProUGUI itemWindowCursorText;
    private RectTransform itemInfoWindowFrame;
    private RectTransform itemInfoWindowContentRoot;
    private TextMeshProUGUI itemEffectDescriptionText;
    private RectTransform cardWindowFrame;
    private RectTransform cardWindowContentRoot;
    private RectTransform cardPreviewRoot;
    private Image cardPreviewImage;
    private CardTemplateView cardPreviewView;
    private TextMeshProUGUI[] cardWindowTexts;
    private TextMeshProUGUI cardWindowCloseText;
    private TextMeshProUGUI cardWindowPageText;
    private TextMeshProUGUI cardWindowCursorText;
    private RectTransform deckWindowFrame;
    private RectTransform deckWindowContentRoot;
    private RectTransform deckPreviewRoot;
    private Image deckPreviewImage;
    private CardTemplateView deckPreviewView;
    private TextMeshProUGUI[] deckWindowTexts;
    private TextMeshProUGUI deckWindowCloseText;
    private TextMeshProUGUI deckWindowPageText;
    private TextMeshProUGUI deckWindowCursorText;
    private RectTransform statusWindowFrame;
    private RectTransform statusWindowContentRoot;
    private TextMeshProUGUI[] statusWindowTexts;
    private TextMeshProUGUI statusWindowCloseText;
    private TextMeshProUGUI statusWindowCursorText;
    private RectTransform exitConfirmWindowFrame;
    private RectTransform exitConfirmContentRoot;
    private TextMeshProUGUI exitConfirmMessageText;
    private TextMeshProUGUI exitConfirmYesText;
    private TextMeshProUGUI exitConfirmNoText;
    private static Sprite generatedFrameSprite;
    private const string DefaultMainMenuSceneName = "Mainmenu";
    private const string DefaultExitConfirmMessage = "메인 메뉴로 돌아가시겠습니까?";
    private const string LegacyExitConfirmMessage = "게임을 완전히 종료하시겠습니까?";
    private const string DefaultExitConfirmNoLabel = "뒤로가기 : Q";
    private const string LegacyExitConfirmNoLabel = "아니요 : Q";
    private static readonly string[] DefaultMenuLabels = { "item", "card", "deck", "option", "status", "exit" };
    private static readonly Vector2 ReferenceItemFramePosition = new Vector2(746f, -50f);
    private static readonly Vector2 ReferenceItemFrameSize = new Vector2(784.8384f, 360f);
    private static readonly Vector2 ReferenceItemContentPosition = new Vector2(783f, -74f);
    private static readonly Vector2 ReferenceItemInfoFramePosition = new Vector2(1544f, -50f);
    private static readonly Vector2 ReferenceItemInfoFrameSize = new Vector2(340f, 362.2f);
    private static readonly Vector2 ReferenceItemInfoContentPosition = new Vector2(1541f, -50f);
    private static readonly Vector2 ReferenceSubWindowFramePosition = new Vector2(749f, -50f);
    private static readonly Vector2 ReferenceSubWindowFrameSize = new Vector2(681.2395f, 360f);
    private static readonly Vector2 ReferenceSubWindowContentPosition = new Vector2(749f, -50f);
    private static readonly Vector2 ReferenceSubWindowContentSize = new Vector2(600f, 300f);
    private static readonly Vector2 ReferencePreviewRootPosition = new Vector2(1446f, -61f);
    private static readonly Vector2 ReferencePreviewRootSize = new Vector2(170f, 240f);
    private static readonly Vector2 ReferencePreviewImagePosition = new Vector2(73.86328f, -50.27759f);
    private static readonly Vector2 ReferencePreviewImageSize = new Vector2(86.1149f, 121.5739f);
    private static readonly Vector2[] ReferenceItemEntryPositions =
    {
        new Vector2(44f, -17f),
        new Vector2(44f, -90f),
        new Vector2(44f, -160f),
        new Vector2(44f, -232f)
    };
    private static readonly Vector2[] ReferenceCardEntryPositions =
    {
        new Vector2(44f, -34f),
        new Vector2(44f, -112f),
        new Vector2(44f, -196f),
        new Vector2(44f, -272f)
    };

    public bool IsOpen => isPaused;

    private void Awake()
    {
        if (Application.isPlaying && !manager) manager = GameManager.Instance;
        NormalizeMenuLabels();
        NormalizeExitSettings();
        ResolveItemDatabase();
        ResolveCardDatabase();
        EnsureRetroMenuView();
    }

    private void OnEnable()
    {
        EnsureRetroMenuView();
        HighlightCurrent();
    }

    private void OnValidate()
    {
        itemEntriesPerPage = Mathf.Max(1, itemEntriesPerPage);
        cardEntriesPerPage = Mathf.Max(1, cardEntriesPerPage);
        cardPreviewCornerFontSize = Mathf.Max(1f, cardPreviewCornerFontSize);
        cardPreviewEffectFontSize = Mathf.Max(1f, cardPreviewEffectFontSize);
        NormalizeMenuLabels();
        NormalizeExitSettings();
        ResolveItemDatabase();
        ResolveCardDatabase();

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

        focusMode = MenuFocusMode.Main;
        itemFocusArea = MenuPanelFocusArea.List;
        cardFocusArea = MenuPanelFocusArea.List;
        deckFocusArea = MenuPanelFocusArea.List;
        itemWindowOpen = false;
        cardWindowOpen = false;
        deckWindowOpen = false;
        statusWindowOpen = false;
        exitConfirmOpen = false;
        itemCurrentPage = 0;
        itemCurrentIndex = 0;
        cardCurrentPage = 0;
        cardCurrentIndex = 0;
        deckCurrentPage = 0;
        deckCurrentIndex = 0;

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

        itemWindowOpen = false;
        cardWindowOpen = false;
        deckWindowOpen = false;
        statusWindowOpen = false;
        focusMode = MenuFocusMode.Main;
        SetItemWindowVisible(false);
        SetCardWindowVisible(false);
        SetDeckWindowVisible(false);
        SetStatusWindowVisible(false);
        exitConfirmOpen = false;
        SetExitConfirmWindowVisible(false);

        if (menuUI) menuUI.SetActive(false);
        isPaused = false;

        if (manager != null) manager.UnlockAction();

        Time.timeScale = 1f;
    }

    public void Navigate(int delta)
    {
        if (!isPaused) return;

        if (focusMode == MenuFocusMode.ExitConfirm && exitConfirmOpen)
            return;

        if (focusMode == MenuFocusMode.Item && itemWindowOpen)
        {
            NavigateItemWindowVertical(delta);
            return;
        }

        if (focusMode == MenuFocusMode.Card && cardWindowOpen)
        {
            NavigateCardWindowVertical(delta);
            return;
        }

        if (focusMode == MenuFocusMode.Deck && deckWindowOpen)
        {
            NavigateDeckWindowVertical(delta);
            return;
        }

        if (focusMode == MenuFocusMode.Status && statusWindowOpen)
        {
            NavigateStatusWindowVertical(delta);
            return;
        }

        if (menuItems == null || menuItems.Length == 0) return;

        if (autoBuildRetroMenu)
            SetCurrentIndex(GetGridNeighbor(0, delta));
        else
            SetCurrentIndex((currentIndex + delta + menuItems.Length) % menuItems.Length);
    }

    public void NavigateHorizontal(int delta)
    {
        if (!isPaused) return;

        if (focusMode == MenuFocusMode.ExitConfirm && exitConfirmOpen)
            return;

        if (focusMode == MenuFocusMode.Item && itemWindowOpen)
        {
            NavigateItemWindowHorizontal(delta);
            return;
        }

        if (focusMode == MenuFocusMode.Card && cardWindowOpen)
        {
            NavigateCardWindowHorizontal(delta);
            return;
        }

        if (focusMode == MenuFocusMode.Deck && deckWindowOpen)
        {
            NavigateDeckWindowHorizontal(delta);
            return;
        }

        if (focusMode == MenuFocusMode.Status && statusWindowOpen)
        {
            NavigateStatusWindowHorizontal(delta);
            return;
        }

        if (menuItems == null || menuItems.Length == 0) return;
        SetCurrentIndex(GetGridNeighbor(delta, 0));
    }

    public void NavigateVertical(int delta)
    {
        Navigate(delta);
    }

    public void SubmitCurrent()
    {
        if (!isPaused) return;

        if (focusMode == MenuFocusMode.ExitConfirm && exitConfirmOpen)
        {
            ConfirmQuitGame();
            return;
        }

        if (focusMode == MenuFocusMode.Item && itemWindowOpen)
        {
            SubmitItemWindow();
            return;
        }

        if (focusMode == MenuFocusMode.Card && cardWindowOpen)
        {
            SubmitCardWindow();
            return;
        }

        if (focusMode == MenuFocusMode.Deck && deckWindowOpen)
        {
            SubmitDeckWindow();
            return;
        }

        if (focusMode == MenuFocusMode.Status && statusWindowOpen)
        {
            SubmitStatusWindow();
            return;
        }

        if (debugMenu) Debug.Log($"[MenuController] Submit index={currentIndex}", this);

        switch (currentIndex)
        {
            case 0: // Item
                OpenItemWindow();
                break;

            case 1: // Card
                OpenCardWindow();
                break;

            case 2: // Deck
                OpenDeckWindow();
                break;

            case 3: // Option
                Debug.Log("[MenuController] Option selected", this);
                break;

            case 4: // Status
                OpenStatusWindow();
                break;

            case 5: // Exit
                OpenExitConfirmWindow();
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
        RefreshItemWindow();
        RefreshCardWindow();
        RefreshDeckWindow();
        RefreshStatusWindow();

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
            case 4: panelText.text = "open status"; break;
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
            ApplyRetroContentLayout();
        retroContentRoot.SetAsLastSibling();

        EnsureRetroMenuItems();
        bool repairCollapsedLayout = IsRetroMenuLayoutCollapsed();
        if (repairCollapsedLayout)
            ApplyRetroContentLayout();

        LayoutMenuItems(repairCollapsedLayout);
        EnsureCursor();
        MoveCursorToCurrentItem();
        EnsureItemWindowView();
        RefreshItemWindow();
        EnsureCardWindowView();
        RefreshCardWindow();
        EnsureDeckWindowView();
        RefreshDeckWindow();
        EnsureStatusWindowView();
        RefreshStatusWindow();
        EnsureExitConfirmWindowView();
        RefreshExitConfirmWindow();
    }

    public void BackOrClose()
    {
        if (!isPaused) return;

        if (focusMode == MenuFocusMode.ExitConfirm && exitConfirmOpen)
        {
            CloseExitConfirmWindow();
            return;
        }

        if (focusMode == MenuFocusMode.Item && itemWindowOpen)
        {
            CloseItemWindow();
            return;
        }

        if (focusMode == MenuFocusMode.Card && cardWindowOpen)
        {
            CloseCardWindow();
            return;
        }

        if (focusMode == MenuFocusMode.Deck && deckWindowOpen)
        {
            CloseDeckWindow();
            return;
        }

        if (focusMode == MenuFocusMode.Status && statusWindowOpen)
        {
            CloseStatusWindow();
            return;
        }

        Close();
    }

    private void NormalizeMenuLabels()
    {
        if (menuLabels == null || menuLabels.Length < DefaultMenuLabels.Length)
        {
            menuLabels = (string[])DefaultMenuLabels.Clone();
            return;
        }

        for (int i = 0; i < DefaultMenuLabels.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(menuLabels[i]))
                menuLabels[i] = DefaultMenuLabels[i];
        }

        if (!string.IsNullOrEmpty(menuLabels[4]) && menuLabels[4].ToLowerInvariant() == "close")
            menuLabels[4] = "status";
    }

    private void NormalizeExitSettings()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            mainMenuSceneName = DefaultMainMenuSceneName;

        if (string.IsNullOrWhiteSpace(exitConfirmMessage) || exitConfirmMessage == LegacyExitConfirmMessage)
            exitConfirmMessage = DefaultExitConfirmMessage;

        if (string.IsNullOrWhiteSpace(exitConfirmNoLabel) || exitConfirmNoLabel == LegacyExitConfirmNoLabel)
            exitConfirmNoLabel = DefaultExitConfirmNoLabel;
    }

    private void OpenExitConfirmWindow()
    {
        if (!autoBuildExitConfirmWindow || menuUI == null || (menuUI.transform as RectTransform) == null)
        {
            ExitToMainMenu();
            return;
        }

        if (debugMenu) Debug.Log("[MenuController] Exit confirm open", this);

        itemWindowOpen = false;
        SetItemWindowVisible(false);
        cardWindowOpen = false;
        SetCardWindowVisible(false);
        deckWindowOpen = false;
        SetDeckWindowVisible(false);
        statusWindowOpen = false;
        SetStatusWindowVisible(false);

        exitConfirmOpen = true;
        focusMode = MenuFocusMode.ExitConfirm;

        EnsureExitConfirmWindowView();
        RefreshExitConfirmWindow();
        HighlightCurrent();
    }

    private void CloseExitConfirmWindow()
    {
        if (debugMenu) Debug.Log("[MenuController] Exit confirm close", this);

        exitConfirmOpen = false;
        focusMode = MenuFocusMode.Main;
        SetExitConfirmWindowVisible(false);
        HighlightCurrent();
    }

    private void ConfirmQuitGame()
    {
        if (debugMenu) Debug.Log("[MenuController] Exit confirmed", this);
        ExitToMainMenu();
    }

    private void EnsureExitConfirmWindowView()
    {
        if (!autoBuildExitConfirmWindow || menuUI == null) return;

        RectTransform menuRoot = menuUI.transform as RectTransform;
        if (menuRoot == null) return;

        bool frameAlreadyExists = menuRoot.Find("ExitConfirmWindowFrame") != null;
        exitConfirmWindowFrame = GetOrCreateRect(menuRoot, "ExitConfirmWindowFrame");
        if (!preserveManualExitConfirmLayout || !frameAlreadyExists)
        {
            exitConfirmWindowFrame.anchorMin = new Vector2(0f, 1f);
            exitConfirmWindowFrame.anchorMax = new Vector2(0f, 1f);
            exitConfirmWindowFrame.pivot = new Vector2(0f, 1f);
            exitConfirmWindowFrame.anchoredPosition = exitConfirmWindowOffsetFromTopLeft;
            exitConfirmWindowFrame.sizeDelta = exitConfirmWindowSize;
            exitConfirmWindowFrame.localScale = Vector3.one;
        }
        exitConfirmWindowFrame.SetAsLastSibling();

        Image frameImage = exitConfirmWindowFrame.GetComponent<Image>();
        if (frameImage == null) frameImage = exitConfirmWindowFrame.gameObject.AddComponent<Image>();
        frameImage.sprite = LoadMenuFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        bool contentAlreadyExists = menuRoot.Find("ExitConfirmWindowContent") != null;
        exitConfirmContentRoot = GetOrCreateRect(menuRoot, "ExitConfirmWindowContent");
        if (!preserveManualExitConfirmLayout || !contentAlreadyExists)
        {
            exitConfirmContentRoot.anchorMin = new Vector2(0f, 1f);
            exitConfirmContentRoot.anchorMax = new Vector2(0f, 1f);
            exitConfirmContentRoot.pivot = new Vector2(0f, 1f);
            exitConfirmContentRoot.anchoredPosition = exitConfirmWindowOffsetFromTopLeft;
            exitConfirmContentRoot.sizeDelta = exitConfirmWindowSize;
            exitConfirmContentRoot.localScale = Vector3.one;
        }
        exitConfirmContentRoot.SetAsLastSibling();

        EnsureExitConfirmWindowTexts();
        SetExitConfirmWindowVisible(exitConfirmOpen || (!Application.isPlaying && previewExitConfirmInEditor));
    }

    private void EnsureExitConfirmWindowTexts()
    {
        if (exitConfirmContentRoot == null) return;

        bool messageAlreadyExists = exitConfirmContentRoot.Find("ExitConfirmMessage") != null;
        exitConfirmMessageText = GetOrCreateText(exitConfirmContentRoot, "ExitConfirmMessage");
        StyleItemInfoText(
            exitConfirmMessageText,
            exitConfirmMessage,
            exitConfirmMessagePosition,
            exitConfirmMessageSize,
            itemFontSize,
            !preserveManualExitConfirmLayout || !messageAlreadyExists
        );
        exitConfirmMessageText.alignment = TextAlignmentOptions.TopLeft;
        exitConfirmMessageText.overflowMode = TextOverflowModes.Overflow;

        bool yesAlreadyExists = exitConfirmContentRoot.Find("ExitConfirmYes") != null;
        exitConfirmYesText = GetOrCreateText(exitConfirmContentRoot, "ExitConfirmYes");
        StyleItemWindowText(
            exitConfirmYesText,
            exitConfirmYesLabel,
            exitConfirmYesPosition,
            exitConfirmAnswerSize,
            !preserveManualExitConfirmLayout || !yesAlreadyExists
        );

        bool noAlreadyExists = exitConfirmContentRoot.Find("ExitConfirmNo") != null;
        exitConfirmNoText = GetOrCreateText(exitConfirmContentRoot, "ExitConfirmNo");
        StyleItemWindowText(
            exitConfirmNoText,
            exitConfirmNoLabel,
            exitConfirmNoPosition,
            exitConfirmAnswerSize,
            !preserveManualExitConfirmLayout || !noAlreadyExists
        );
    }

    private void RefreshExitConfirmWindow()
    {
        if (!autoBuildExitConfirmWindow || exitConfirmContentRoot == null) return;

        bool shouldShow = exitConfirmOpen || (!Application.isPlaying && previewExitConfirmInEditor);
        SetExitConfirmWindowVisible(shouldShow);
        if (!shouldShow) return;

        if (exitConfirmWindowFrame != null)
            exitConfirmWindowFrame.SetAsLastSibling();

        if (exitConfirmContentRoot != null)
            exitConfirmContentRoot.SetAsLastSibling();

        if (exitConfirmMessageText != null)
            exitConfirmMessageText.text = exitConfirmMessage;

        if (exitConfirmYesText != null)
            exitConfirmYesText.text = exitConfirmYesLabel;

        if (exitConfirmNoText != null)
            exitConfirmNoText.text = exitConfirmNoLabel;
    }

    private void SetExitConfirmWindowVisible(bool visible)
    {
        if (exitConfirmWindowFrame != null)
            exitConfirmWindowFrame.gameObject.SetActive(visible);

        if (exitConfirmContentRoot != null)
            exitConfirmContentRoot.gameObject.SetActive(visible);
    }

    private void ExitToMainMenu()
    {
        Debug.Log($"[MenuController] Exit selected -> {mainMenuSceneName}", this);

        if (isPaused)
            Close();

        SceneTransitionService.LoadDefault(mainMenuSceneName, useFaderIfExists);
    }

    private void ResolveItemDatabase()
    {
        if (itemDatabase != null) return;

        ItemDatabaseSO resourceDatabase = Resources.Load<ItemDatabaseSO>("ItemDatabaseSO");
        if (resourceDatabase == null)
            resourceDatabase = Resources.Load<ItemDatabaseSO>("ItemDefinitions/ItemDatabaseSO");

        if (resourceDatabase != null)
        {
            itemDatabase = resourceDatabase;
            return;
        }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDatabaseSO");
        if (guids == null || guids.Length == 0) return;

        string selectedPath = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith("/ItemDatabaseSO.asset"))
            {
                selectedPath = path;
                break;
            }

            selectedPath ??= path;
        }

        if (string.IsNullOrEmpty(selectedPath)) return;

        itemDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(selectedPath);
        if (itemDatabase != null && !Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void ResolveCardDatabase()
    {
        if (cardDatabase != null) return;

        var orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (orchestrator != null && orchestrator.CardDatabase != null)
        {
            cardDatabase = orchestrator.CardDatabase;
            return;
        }

        CardDatabaseSO resourceDatabase = Resources.Load<CardDatabaseSO>("CardDatabase");
        if (resourceDatabase != null)
        {
            cardDatabase = resourceDatabase;
            return;
        }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardDatabaseSO");
        if (guids == null || guids.Length == 0) return;

        string selectedPath = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith("/CardDatabase.asset"))
            {
                selectedPath = path;
                break;
            }

            selectedPath ??= path;
        }

        if (string.IsNullOrEmpty(selectedPath)) return;

        cardDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDatabaseSO>(selectedPath);
        if (cardDatabase != null && !Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void OpenItemWindow()
    {
        if (!autoBuildItemWindow)
        {
            Debug.Log("[MenuController] Item selected (UI disabled)", this);
            return;
        }

        if (debugMenu) Debug.Log("[MenuController] Item window open", this);

        cardWindowOpen = false;
        SetCardWindowVisible(false);
        deckWindowOpen = false;
        SetDeckWindowVisible(false);
        statusWindowOpen = false;
        SetStatusWindowVisible(false);

        itemWindowOpen = true;
        focusMode = MenuFocusMode.Item;
        itemFocusArea = MenuPanelFocusArea.List;
        itemCurrentIndex = Mathf.Clamp(itemCurrentIndex, 0, GetCurrentItemLabelCount() - 1);
        itemCurrentPage = Mathf.Clamp(itemCurrentPage, 0, GetItemPageCount() - 1);

        EnsureItemWindowView();
        RefreshItemWindow();
        HighlightCurrent();
    }

    private void CloseItemWindow()
    {
        if (debugMenu) Debug.Log("[MenuController] Item window close", this);

        itemWindowOpen = false;
        focusMode = MenuFocusMode.Main;
        itemFocusArea = MenuPanelFocusArea.List;
        SetItemWindowVisible(false);
        HighlightCurrent();
    }

    private void NavigateItemWindowHorizontal(int delta)
    {
        if (delta == 0) return;

        MenuPanelFocusArea nextFocus = itemFocusArea;
        if (delta > 0 && itemFocusArea == MenuPanelFocusArea.List)
            nextFocus = MenuPanelFocusArea.Close;
        else if (delta < 0 && itemFocusArea != MenuPanelFocusArea.List)
            nextFocus = MenuPanelFocusArea.List;

        if (itemFocusArea == nextFocus) return;

        itemFocusArea = nextFocus;
        if (debugMenu) Debug.Log($"[MenuController] Item focus -> {itemFocusArea}", this);

        RefreshItemWindow();
    }

    private void NavigateItemWindowVertical(int delta)
    {
        if (delta == 0) return;

        if (itemFocusArea == MenuPanelFocusArea.Close)
        {
            if (delta > 0)
            {
                itemFocusArea = MenuPanelFocusArea.Page;
                if (debugMenu) Debug.Log($"[MenuController] Item focus -> {itemFocusArea}", this);
                RefreshItemWindow();
            }

            return;
        }

        if (itemFocusArea == MenuPanelFocusArea.Page)
        {
            if (delta < 0)
            {
                itemFocusArea = MenuPanelFocusArea.Close;
                if (debugMenu) Debug.Log($"[MenuController] Item focus -> {itemFocusArea}", this);
                RefreshItemWindow();
            }

            return;
        }

        int count = GetCurrentItemLabelCount();
        if (count <= 0) return;

        itemCurrentIndex = (itemCurrentIndex + delta + count) % count;
        if (debugMenu) Debug.Log($"[MenuController] Item navigate -> {itemCurrentIndex}", this);

        RefreshItemWindow();
    }

    private void SubmitItemWindow()
    {
        if (itemFocusArea == MenuPanelFocusArea.Close)
        {
            CloseItemWindow();
            return;
        }

        if (itemFocusArea == MenuPanelFocusArea.Page)
        {
            AdvanceItemPage();
            return;
        }

        UseSelectedItem();
    }

    private void UseSelectedItem()
    {
        ItemMenuEntry entry = GetSelectedItemEntry();
        if (string.IsNullOrEmpty(entry.id))
            return;

        ItemRuntime runtime = EnsureItemRuntimeForMenu();
        if (runtime == null)
        {
            Debug.LogWarning("[MenuController] ItemRuntime is missing. Cannot use item.", this);
            return;
        }

        if (runtime.GetQuantity(entry.id) <= 0)
        {
            RefreshItemWindow();
            return;
        }

        ItemSO item = entry.item;
        if (item == null)
        {
            ResolveItemDatabase();
            item = itemDatabase != null ? itemDatabase.GetById(entry.id) : null;
        }

        if (item == null)
        {
            Debug.LogWarning($"[MenuController] ItemSO not found for id '{entry.id}'.", this);
            RefreshItemWindow();
            return;
        }

        if (!item.TryUse(out string message))
        {
            if (!string.IsNullOrEmpty(message))
                Debug.LogWarning($"[MenuController] {message}", this);

            RefreshItemWindow();
            RefreshStatusWindow();
            return;
        }

        if (item.consumeOnUse)
            runtime.AddQuantity(entry.id, -1);

        ShowItemUseDialogue(item, entry.id);

        itemCurrentPage = Mathf.Clamp(itemCurrentPage, 0, GetItemPageCount() - 1);
        itemCurrentIndex = Mathf.Clamp(itemCurrentIndex, 0, GetCurrentItemLabelCount() - 1);

        if (debugMenu) Debug.Log($"[MenuController] Used item: {entry.id}", this);

        RefreshItemWindow();
        RefreshStatusWindow();
    }

    private void ShowItemUseDialogue(ItemSO item, string fallbackId)
    {
        DialogueManager dm = DialogueManager.instance;
        if (dm == null)
        {
            Debug.LogWarning("[MenuController] DialogueManager is missing. Cannot show item use dialogue.", this);
            return;
        }

        dm.blockInput = false;
        dm.StartDialogue(new Dialogue
        {
            name = "",
            lines = BuildItemUseDialogueLines(item, fallbackId)
        });
        dm.ForceCompleteTyping();
    }

    private DialogueLine[] BuildItemUseDialogueLines(ItemSO item, string fallbackId)
    {
        List<DialogueLine> lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                text = $"\"{GetItemDisplayName(item, fallbackId)}\"을 사용했습니다",
                focus = PortraitFocus.None
            }
        };

        if (item != null && !string.IsNullOrWhiteSpace(item.useText))
        {
            lines.Add(new DialogueLine
            {
                text = item.useText.Trim(),
                focus = PortraitFocus.None
            });
        }

        return lines.ToArray();
    }

    private void AdvanceItemPage()
    {
        int pageCount = GetItemPageCount();
        if (pageCount <= 1)
        {
            RefreshItemWindow();
            return;
        }

        itemCurrentPage = (itemCurrentPage + 1) % pageCount;
        itemCurrentIndex = Mathf.Clamp(itemCurrentIndex, 0, GetCurrentItemLabelCount() - 1);

        if (debugMenu) Debug.Log($"[MenuController] Item page -> {itemCurrentPage + 1}/{pageCount}", this);

        RefreshItemWindow();
    }

    private void EnsureItemWindowView()
    {
        if (!autoBuildItemWindow || menuUI == null) return;

        RectTransform menuRoot = menuUI.transform as RectTransform;
        if (menuRoot == null) return;

        bool frameAlreadyExists = menuRoot.Find("ItemWindowFrame") != null;
        itemWindowFrame = GetOrCreateRect(menuRoot, "ItemWindowFrame");
        if (!preserveManualItemLayout || !frameAlreadyExists)
        {
            itemWindowFrame.anchorMin = new Vector2(0f, 1f);
            itemWindowFrame.anchorMax = new Vector2(0f, 1f);
            itemWindowFrame.pivot = new Vector2(0f, 1f);
            itemWindowFrame.anchoredPosition = itemWindowOffsetFromTopLeft;
            itemWindowFrame.sizeDelta = itemWindowSize;
            itemWindowFrame.localScale = Vector3.one;
        }
        itemWindowFrame.SetAsLastSibling();

        Image frameImage = itemWindowFrame.GetComponent<Image>();
        if (frameImage == null) frameImage = itemWindowFrame.gameObject.AddComponent<Image>();
        frameImage.sprite = LoadMenuFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        bool contentAlreadyExists = menuRoot.Find("ItemWindowContent") != null;
        itemWindowContentRoot = GetOrCreateRect(menuRoot, "ItemWindowContent");
        if (!preserveManualItemLayout || !contentAlreadyExists)
        {
            itemWindowContentRoot.anchorMin = new Vector2(0f, 1f);
            itemWindowContentRoot.anchorMax = new Vector2(0f, 1f);
            itemWindowContentRoot.pivot = new Vector2(0f, 1f);
            itemWindowContentRoot.anchoredPosition = itemWindowOffsetFromTopLeft;
            itemWindowContentRoot.sizeDelta = itemWindowSize;
            itemWindowContentRoot.localScale = Vector3.one;
        }
        itemWindowContentRoot.SetAsLastSibling();

        EnsureItemInfoWindowView(menuRoot);
        if (IsLegacyItemWindowLayout())
            ApplyReferenceItemWindowLayout();

        EnsureItemWindowTexts();
        SetItemWindowVisible(itemWindowOpen || (!Application.isPlaying && previewItemWindowInEditor));
    }

    private void EnsureItemInfoWindowView(RectTransform menuRoot)
    {
        if (menuRoot == null) return;

        bool frameAlreadyExists = menuRoot.Find("ItemInfoWindowFrame") != null;
        itemInfoWindowFrame = GetOrCreateRect(menuRoot, "ItemInfoWindowFrame");
        if (!preserveManualItemLayout || !frameAlreadyExists)
        {
            itemInfoWindowFrame.anchorMin = new Vector2(0f, 1f);
            itemInfoWindowFrame.anchorMax = new Vector2(0f, 1f);
            itemInfoWindowFrame.pivot = new Vector2(0f, 1f);
            itemInfoWindowFrame.anchoredPosition = itemInfoWindowOffsetFromTopLeft;
            itemInfoWindowFrame.sizeDelta = itemInfoWindowSize;
            itemInfoWindowFrame.localScale = Vector3.one;
        }
        itemInfoWindowFrame.SetAsLastSibling();

        Image frameImage = itemInfoWindowFrame.GetComponent<Image>();
        if (frameImage == null) frameImage = itemInfoWindowFrame.gameObject.AddComponent<Image>();
        frameImage.sprite = LoadMenuFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        bool contentAlreadyExists = menuRoot.Find("ItemInfoWindowContent") != null;
        itemInfoWindowContentRoot = GetOrCreateRect(menuRoot, "ItemInfoWindowContent");
        if (!preserveManualItemLayout || !contentAlreadyExists)
        {
            itemInfoWindowContentRoot.anchorMin = new Vector2(0f, 1f);
            itemInfoWindowContentRoot.anchorMax = new Vector2(0f, 1f);
            itemInfoWindowContentRoot.pivot = new Vector2(0f, 1f);
            itemInfoWindowContentRoot.anchoredPosition = itemInfoWindowOffsetFromTopLeft;
            itemInfoWindowContentRoot.sizeDelta = itemInfoWindowSize;
            itemInfoWindowContentRoot.localScale = Vector3.one;
        }
        itemInfoWindowContentRoot.SetAsLastSibling();

        EnsureItemInfoWindowTexts();
    }

    private void EnsureItemWindowTexts()
    {
        if (itemWindowContentRoot == null) return;

        int entryCount = GetItemWindowEntryCount();
        TextMeshProUGUI[] entries = new TextMeshProUGUI[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            string entryName = $"ItemEntry_{i}";
            bool alreadyExists = itemWindowContentRoot.Find(entryName) != null;
            entries[i] = GetOrCreateText(itemWindowContentRoot, entryName);

            StyleItemWindowText(
                entries[i],
                GetItemLabel(i),
                GetItemEntryPosition(i),
                itemWindowEntrySize,
                !preserveManualItemLayout || !alreadyExists
            );
        }

        itemWindowTexts = entries;

        bool closeAlreadyExists = itemWindowContentRoot.Find("ItemClose") != null;
        itemWindowCloseText = GetOrCreateText(itemWindowContentRoot, "ItemClose");
        StyleItemWindowText(
            itemWindowCloseText,
            itemCloseLabel,
            itemWindowClosePosition,
            itemWindowCloseSize,
            !preserveManualItemLayout || !closeAlreadyExists
        );

        bool pageAlreadyExists = itemWindowContentRoot.Find("ItemPage") != null;
        itemWindowPageText = GetOrCreateText(itemWindowContentRoot, "ItemPage");
        StyleItemWindowText(
            itemWindowPageText,
            GetItemPageText(),
            itemWindowPagePosition,
            itemWindowPageSize,
            !preserveManualItemLayout || !pageAlreadyExists
        );

        HideLegacyItemMessageText();

        bool cursorAlreadyExists = itemWindowContentRoot.Find("ItemCursor") != null;
        itemWindowCursorText = GetOrCreateText(itemWindowContentRoot, "ItemCursor");
        StyleItemWindowText(
            itemWindowCursorText,
            "<",
            itemWindowListOrigin,
            new Vector2(46f, itemWindowEntrySize.y),
            !preserveManualItemLayout || !cursorAlreadyExists
        );
        itemWindowCursorText.alignment = TextAlignmentOptions.Center;
    }

    private void EnsureItemInfoWindowTexts()
    {
        if (itemInfoWindowContentRoot == null) return;

        float horizontalPadding = 30f;
        float availableWidth = Mathf.Max(80f, itemInfoWindowSize.x - horizontalPadding * 2f);

        HideItemInfoLegacyTexts();

        bool descriptionAlreadyExists = itemInfoWindowContentRoot.Find("ItemEffectDescription") != null;
        itemEffectDescriptionText = GetOrCreateText(itemInfoWindowContentRoot, "ItemEffectDescription");
        StyleItemInfoText(
            itemEffectDescriptionText,
            "설명",
            new Vector2(horizontalPadding, -34f),
            new Vector2(availableWidth, Mathf.Max(80f, itemInfoWindowSize.y - 68f)),
            itemFontSize,
            !preserveManualItemLayout || !descriptionAlreadyExists
        );
        itemEffectDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        itemEffectDescriptionText.enableWordWrapping = true;
        itemEffectDescriptionText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void HideItemInfoLegacyTexts()
    {
        if (itemInfoWindowContentRoot == null) return;

        HideChild(itemInfoWindowContentRoot, "ItemInfoName");
        HideChild(itemInfoWindowContentRoot, "ItemInfoEffect");
        HideChild(itemInfoWindowContentRoot, "ItemInfoDescription");
    }

    private void RefreshItemWindow()
    {
        if (!autoBuildItemWindow || itemWindowContentRoot == null) return;

        itemCurrentPage = Mathf.Clamp(itemCurrentPage, 0, GetItemPageCount() - 1);
        itemCurrentIndex = Mathf.Clamp(itemCurrentIndex, 0, GetCurrentItemLabelCount() - 1);

        bool shouldShow = itemWindowOpen || (!Application.isPlaying && previewItemWindowInEditor);
        SetItemWindowVisible(shouldShow);
        if (!shouldShow) return;

        if (itemWindowTexts != null)
        {
            string[] labels = GetCurrentItemLabels();
            for (int i = 0; i < itemWindowTexts.Length; i++)
            {
                if (itemWindowTexts[i] == null) continue;

                string label = i < labels.Length ? labels[i] : "";
                itemWindowTexts[i].text = label;
                itemWindowTexts[i].gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }

        if (itemWindowCloseText != null)
            itemWindowCloseText.text = string.IsNullOrEmpty(itemCloseLabel) ? "close" : itemCloseLabel;

        if (itemWindowPageText != null)
            itemWindowPageText.text = GetItemPageText();

        HideLegacyItemMessageText();
        MoveItemWindowCursor();
        RefreshItemInfoWindow();
    }

    private void RefreshItemInfoWindow()
    {
        if (itemInfoWindowContentRoot == null) return;

        bool shouldShow = itemWindowOpen || (!Application.isPlaying && previewItemWindowInEditor);
        itemInfoWindowContentRoot.gameObject.SetActive(shouldShow);
        if (itemInfoWindowFrame != null)
            itemInfoWindowFrame.gameObject.SetActive(shouldShow);
        if (!shouldShow) return;

        ItemMenuEntry entry = GetSelectedItemEntry();
        ItemSO item = ResolveItemDefinition(entry);

        HideItemInfoLegacyTexts();

        if (itemEffectDescriptionText != null)
            itemEffectDescriptionText.text = GetItemEffectDescriptionText(entry, item);
    }

    private void HideLegacyItemMessageText()
    {
        if (itemWindowContentRoot == null) return;

        HideChild(itemWindowContentRoot, "ItemMessage");
    }

    private static void HideChild(RectTransform parent, string childName)
    {
        if (parent == null) return;

        Transform child = parent.Find(childName);
        if (child != null)
            child.gameObject.SetActive(false);
    }

    private void MoveItemWindowCursor()
    {
        if (itemWindowCursorText == null)
            return;

        bool shouldShow = itemWindowOpen || (!Application.isPlaying && previewItemWindowInEditor);
        itemWindowCursorText.gameObject.SetActive(shouldShow);
        if (!shouldShow) return;

        TextMeshProUGUI targetText = GetItemFocusText();

        if (targetText == null)
            return;

        float textWidth = targetText.GetPreferredValues(targetText.text).x;
        Vector2 targetPosition = targetText.rectTransform.anchoredPosition;

        itemWindowCursorText.text = "<";
        itemWindowCursorText.rectTransform.anchoredPosition = new Vector2(
            targetPosition.x + textWidth + cursorGapAfterText,
            targetPosition.y + 1f
        );
        itemWindowCursorText.transform.SetAsLastSibling();
    }

    private TextMeshProUGUI GetItemFocusText()
    {
        if (itemFocusArea == MenuPanelFocusArea.Close)
            return itemWindowCloseText;

        if (itemFocusArea == MenuPanelFocusArea.Page)
            return itemWindowPageText;

        return GetCurrentItemText();
    }

    private TextMeshProUGUI GetCurrentItemText()
    {
        if (itemWindowTexts == null || itemWindowTexts.Length == 0) return null;

        int safeIndex = Mathf.Clamp(itemCurrentIndex, 0, itemWindowTexts.Length - 1);
        return itemWindowTexts[safeIndex];
    }

    private TextMeshProUGUI GetOrCreateText(RectTransform parent, string childName)
    {
        RectTransform rect = GetOrCreateRect(parent, childName);
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset font = GetRetroFont();
        if (font != null)
            text.font = font;

        return text;
    }

    private void StyleItemWindowText(
        TextMeshProUGUI text,
        string value,
        Vector2 anchoredPosition,
        Vector2 size,
        bool applyDefaultTransform
    )
    {
        if (text == null) return;

        text.text = value;
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

        TMP_FontAsset font = GetRetroFont();
        if (font != null)
            text.font = font;

        if (!applyDefaultTransform)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void StyleItemInfoText(
        TextMeshProUGUI text,
        string value,
        Vector2 anchoredPosition,
        Vector2 size,
        float maxFontSize,
        bool applyDefaultTransform
    )
    {
        if (text == null) return;

        StyleItemWindowText(text, value, anchoredPosition, size, applyDefaultTransform);
        text.enableAutoSizing = true;
        text.fontSizeMin = 22f;
        text.fontSizeMax = Mathf.Max(22f, maxFontSize);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void CopyRectFromSourceOrDefault(
        RectTransform target,
        RectTransform source,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        if (target == null) return;

        if (source != null)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.localScale = source.localScale;
            return;
        }

        target.anchorMin = new Vector2(0f, 1f);
        target.anchorMax = new Vector2(0f, 1f);
        target.pivot = new Vector2(0f, 1f);
        target.anchoredPosition = anchoredPosition;
        target.sizeDelta = size;
        target.localScale = Vector3.one;
    }

    private bool IsLegacyItemWindowLayout()
    {
        if (!repairLegacySubWindowLayout) return false;

        return IsNear(itemWindowFrame, itemWindowOffsetFromTopLeft, itemWindowSize)
            || IsNear(itemWindowContentRoot, itemWindowOffsetFromTopLeft, itemWindowSize)
            || IsNear(itemInfoWindowFrame, itemInfoWindowOffsetFromTopLeft, itemInfoWindowSize)
            || IsNear(itemInfoWindowContentRoot, itemInfoWindowOffsetFromTopLeft, itemInfoWindowSize);
    }

    private bool IsLegacySubWindowLayout(
        RectTransform frame,
        RectTransform content,
        RectTransform previewRoot,
        RectTransform previewImage
    )
    {
        if (!repairLegacySubWindowLayout) return false;

        return IsNear(frame, cardWindowOffsetFromTopLeft, cardWindowSize)
            || IsNear(frame, statusWindowOffsetFromTopLeft, statusWindowSize)
            || IsNear(content, cardWindowOffsetFromTopLeft, cardWindowSize)
            || IsNear(content, statusWindowOffsetFromTopLeft, statusWindowSize)
            || IsNear(previewRoot, cardPreviewOffsetFromTopLeft, cardPreviewSize)
            || IsCollapsedPreviewImage(previewImage);
    }

    private static bool IsNear(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null) return false;

        return (rect.anchoredPosition - anchoredPosition).sqrMagnitude <= 1f
            && (rect.sizeDelta - size).sqrMagnitude <= 1f;
    }

    private static bool IsCollapsedPreviewImage(RectTransform rect)
    {
        return rect != null && rect.sizeDelta.sqrMagnitude <= 1f;
    }

    private void ApplyReferenceItemWindowLayout()
    {
        ApplyTopLeftRect(itemWindowFrame, ReferenceItemFramePosition, ReferenceItemFrameSize);
        ApplyTopLeftRect(itemWindowContentRoot, ReferenceItemContentPosition, ReferenceSubWindowContentSize);
        ApplyTopLeftRect(itemInfoWindowFrame, ReferenceItemInfoFramePosition, ReferenceItemInfoFrameSize);
        ApplyTopLeftRect(itemInfoWindowContentRoot, ReferenceItemInfoContentPosition, ReferenceSubWindowContentSize);

        for (int i = 0; i < ReferenceItemEntryPositions.Length; i++)
            ApplyChildTopLeftTextRect(itemWindowContentRoot, $"ItemEntry_{i}", ReferenceItemEntryPositions[i], itemWindowEntrySize);

        ApplyChildTopLeftTextRect(itemWindowContentRoot, "ItemClose", new Vector2(542f, -20f), itemWindowCloseSize);
        ApplyChildTopLeftTextRect(itemWindowContentRoot, "ItemPage", new Vector2(546f, -233f), itemWindowPageSize);
        ApplyChildTopLeftTextRect(itemWindowContentRoot, "ItemCursor", new Vector2(44f, -52f), new Vector2(46f, itemWindowEntrySize.y));
        ApplyChildTopLeftTextRect(itemInfoWindowContentRoot, "ItemEffectDescription", new Vector2(30f, -34f), new Vector2(280f, 232f));

        if (itemWindowFrame != null) itemWindowFrame.SetAsLastSibling();
        if (itemWindowContentRoot != null) itemWindowContentRoot.SetAsLastSibling();
        if (itemInfoWindowFrame != null) itemInfoWindowFrame.SetAsLastSibling();
        if (itemInfoWindowContentRoot != null) itemInfoWindowContentRoot.SetAsLastSibling();
    }

    private void ApplyReferenceCardWindowLayout()
    {
        ApplyReferenceCardLikeWindowLayout(
            cardWindowFrame,
            cardWindowContentRoot,
            cardPreviewRoot,
            cardPreviewImage != null ? cardPreviewImage.rectTransform : null,
            "Card"
        );
    }

    private void ApplyReferenceDeckWindowLayout()
    {
        ApplyReferenceCardLikeWindowLayout(
            deckWindowFrame,
            deckWindowContentRoot,
            deckPreviewRoot,
            deckPreviewImage != null ? deckPreviewImage.rectTransform : null,
            "Deck"
        );
    }

    private void ApplyReferenceStatusWindowLayout()
    {
        ApplyTopLeftRect(statusWindowFrame, ReferenceSubWindowFramePosition, ReferenceSubWindowFrameSize);
        ApplyTopLeftRect(statusWindowContentRoot, ReferenceSubWindowContentPosition, ReferenceSubWindowContentSize);

        int statusCount = Mathf.Max(5, GetStatusLabels().Length);
        for (int i = 0; i < statusCount; i++)
        {
            Vector2 position = new Vector2(statusWindowListOrigin.x, statusWindowListOrigin.y - i * statusWindowRowSpacing);
            ApplyChildTopLeftTextRect(statusWindowContentRoot, $"StatusEntry_{i}", position, statusWindowEntrySize);
        }

        ApplyChildTopLeftTextRect(statusWindowContentRoot, "StatusClose", statusWindowClosePosition, statusWindowCloseSize);
        ApplyChildTopLeftTextRect(statusWindowContentRoot, "StatusCursor", statusWindowClosePosition, new Vector2(46f, statusWindowCloseSize.y));

        if (statusWindowFrame != null) statusWindowFrame.SetAsLastSibling();
        if (statusWindowContentRoot != null) statusWindowContentRoot.SetAsLastSibling();
    }

    private void ApplyReferenceCardLikeWindowLayout(
        RectTransform frame,
        RectTransform content,
        RectTransform previewRoot,
        RectTransform previewImage,
        string prefix
    )
    {
        ApplyTopLeftRect(frame, ReferenceSubWindowFramePosition, ReferenceSubWindowFrameSize);
        ApplyTopLeftRect(content, ReferenceSubWindowContentPosition, ReferenceSubWindowContentSize);
        ApplyTopLeftRect(previewRoot, ReferencePreviewRootPosition, ReferencePreviewRootSize);
        ApplyPreviewImageRect(previewImage);

        for (int i = 0; i < ReferenceCardEntryPositions.Length; i++)
            ApplyChildTopLeftTextRect(content, $"{prefix}Entry_{i}", ReferenceCardEntryPositions[i], cardWindowEntrySize);

        ApplyChildTopLeftTextRect(content, $"{prefix}Close", new Vector2(498f, -34f), cardWindowCloseSize);
        ApplyChildTopLeftTextRect(content, $"{prefix}Page", new Vector2(498f, -272f), cardWindowPageSize);
        ApplyChildTopLeftTextRect(content, $"{prefix}Cursor", new Vector2(44f, -52f), new Vector2(46f, cardWindowEntrySize.y));

        if (frame != null) frame.SetAsLastSibling();
        if (content != null) content.SetAsLastSibling();
        if (previewRoot != null) previewRoot.SetAsLastSibling();
    }

    private void ApplyChildTopLeftTextRect(RectTransform parent, string childName, Vector2 anchoredPosition, Vector2 size)
    {
        if (parent == null) return;

        TextMeshProUGUI text = GetOrCreateText(parent, childName);
        ApplyTopLeftRect(text.rectTransform, anchoredPosition, size);
    }

    private static void ApplyTopLeftRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void ApplyPreviewImageRect(RectTransform rect)
    {
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = ReferencePreviewImagePosition;
        rect.sizeDelta = ReferencePreviewImageSize;
        rect.localScale = Vector3.one;
    }

    private void CopyTextLayoutFromCard(TextMeshProUGUI target, string cardChildName)
    {
        if (target == null || cardWindowContentRoot == null) return;

        Transform sourceTransform = cardWindowContentRoot.Find(cardChildName);
        if (sourceTransform == null) return;

        RectTransform sourceRect = sourceTransform as RectTransform;
        CopyRectFromSourceOrDefault(target.rectTransform, sourceRect, target.rectTransform.anchoredPosition, target.rectTransform.sizeDelta);

        TextMeshProUGUI sourceText = sourceTransform.GetComponent<TextMeshProUGUI>();
        if (sourceText == null) return;

        target.font = sourceText.font;
        target.fontSize = sourceText.fontSize;
        target.enableAutoSizing = sourceText.enableAutoSizing;
        target.fontSizeMin = sourceText.fontSizeMin;
        target.fontSizeMax = sourceText.fontSizeMax;
        target.fontStyle = sourceText.fontStyle;
        target.alignment = sourceText.alignment;
        target.enableWordWrapping = sourceText.enableWordWrapping;
        target.overflowMode = sourceText.overflowMode;
        target.color = sourceText.color;
        target.characterSpacing = sourceText.characterSpacing;
        target.richText = sourceText.richText;
        target.raycastTarget = false;
    }

    private void SetItemWindowVisible(bool visible)
    {
        if (itemWindowFrame != null)
            itemWindowFrame.gameObject.SetActive(visible);

        if (itemWindowContentRoot != null)
            itemWindowContentRoot.gameObject.SetActive(visible);

        if (itemInfoWindowFrame != null)
            itemInfoWindowFrame.gameObject.SetActive(visible);

        if (itemInfoWindowContentRoot != null)
            itemInfoWindowContentRoot.gameObject.SetActive(visible);
    }

    private void OpenCardWindow()
    {
        if (!autoBuildCardWindow)
        {
            Debug.Log("[MenuController] Card selected (UI disabled)", this);
            return;
        }

        if (debugMenu) Debug.Log("[MenuController] Card window open", this);

        itemWindowOpen = false;
        SetItemWindowVisible(false);
        deckWindowOpen = false;
        SetDeckWindowVisible(false);
        statusWindowOpen = false;
        SetStatusWindowVisible(false);

        cardWindowOpen = true;
        focusMode = MenuFocusMode.Card;
        cardFocusArea = MenuPanelFocusArea.List;
        cardCurrentIndex = Mathf.Clamp(cardCurrentIndex, 0, GetCurrentCardLabelCount() - 1);
        cardCurrentPage = Mathf.Clamp(cardCurrentPage, 0, GetCardPageCount() - 1);

        EnsureCardWindowView();
        RefreshCardWindow();
        HighlightCurrent();
    }

    private void CloseCardWindow()
    {
        if (debugMenu) Debug.Log("[MenuController] Card window close", this);

        cardWindowOpen = false;
        focusMode = MenuFocusMode.Main;
        cardFocusArea = MenuPanelFocusArea.List;
        SetCardWindowVisible(false);
        HighlightCurrent();
    }

    private void NavigateCardWindowHorizontal(int delta)
    {
        if (delta == 0) return;

        MenuPanelFocusArea nextFocus = cardFocusArea;
        if (delta > 0 && cardFocusArea == MenuPanelFocusArea.List)
            nextFocus = MenuPanelFocusArea.Close;
        else if (delta < 0 && cardFocusArea != MenuPanelFocusArea.List)
            nextFocus = MenuPanelFocusArea.List;

        if (cardFocusArea == nextFocus) return;

        cardFocusArea = nextFocus;
        if (debugMenu) Debug.Log($"[MenuController] Card focus -> {cardFocusArea}", this);

        RefreshCardWindow();
    }

    private void NavigateCardWindowVertical(int delta)
    {
        if (delta == 0) return;

        if (cardFocusArea == MenuPanelFocusArea.Close)
        {
            if (delta > 0)
            {
                cardFocusArea = MenuPanelFocusArea.Page;
                if (debugMenu) Debug.Log($"[MenuController] Card focus -> {cardFocusArea}", this);
                RefreshCardWindow();
            }

            return;
        }

        if (cardFocusArea == MenuPanelFocusArea.Page)
        {
            if (delta < 0)
            {
                cardFocusArea = MenuPanelFocusArea.Close;
                if (debugMenu) Debug.Log($"[MenuController] Card focus -> {cardFocusArea}", this);
                RefreshCardWindow();
            }

            return;
        }

        int count = GetCurrentCardLabelCount();
        if (count <= 0) return;

        cardCurrentIndex = (cardCurrentIndex + delta + count) % count;
        if (debugMenu) Debug.Log($"[MenuController] Card navigate -> {cardCurrentIndex}", this);

        RefreshCardWindow();
    }

    private void SubmitCardWindow()
    {
        if (cardFocusArea == MenuPanelFocusArea.Close)
        {
            CloseCardWindow();
            return;
        }

        if (cardFocusArea == MenuPanelFocusArea.Page)
        {
            AdvanceCardPage();
            return;
        }

        AddSelectedCardToDeck();
    }

    private void AdvanceCardPage()
    {
        int pageCount = GetCardPageCount();
        if (pageCount <= 1)
        {
            RefreshCardWindow();
            return;
        }

        cardCurrentPage = (cardCurrentPage + 1) % pageCount;
        cardCurrentIndex = Mathf.Clamp(cardCurrentIndex, 0, GetCurrentCardLabelCount() - 1);

        if (debugMenu) Debug.Log($"[MenuController] Card page -> {cardCurrentPage + 1}/{pageCount}", this);

        RefreshCardWindow();
    }

    private void AddSelectedCardToDeck()
    {
        CardMenuEntry entry = GetSelectedCardEntry();
        if (string.IsNullOrEmpty(entry.id))
            return;

        CardStateRuntime runtime = EnsureCardStateRuntimeForMenu();
        if (runtime == null)
        {
            Debug.LogWarning("[MenuController] CardStateRuntime is missing. Cannot add card to deck.", this);
            return;
        }

        if (!runtime.TryAddToDeck(entry.id))
        {
            if (debugMenu)
            {
                string reason = runtime.DeckContains(entry.id)
                    ? "already in deck"
                    : $"deck full ({runtime.DeckCount}/{CardStateRuntime.MAX_DECK})";
                Debug.Log($"[MenuController] Add card skipped: {entry.id} ({reason})", this);
            }

            RefreshCardWindow();
            RefreshDeckWindow();
            return;
        }

        runtime.SaveNow();
        cardCurrentPage = Mathf.Clamp(cardCurrentPage, 0, GetCardPageCount() - 1);
        cardCurrentIndex = Mathf.Clamp(cardCurrentIndex, 0, GetCurrentCardLabelCount() - 1);

        if (debugMenu) Debug.Log($"[MenuController] Added card to deck: {entry.id}", this);

        RefreshCardWindow();
        RefreshDeckWindow();
    }

    private void EnsureCardWindowView()
    {
        if (!autoBuildCardWindow || menuUI == null) return;

        RectTransform menuRoot = menuUI.transform as RectTransform;
        if (menuRoot == null) return;

        bool frameAlreadyExists = menuRoot.Find("CardWindowFrame") != null;
        cardWindowFrame = GetOrCreateRect(menuRoot, "CardWindowFrame");
        if (!preserveManualCardLayout || !frameAlreadyExists)
        {
            cardWindowFrame.anchorMin = new Vector2(0f, 1f);
            cardWindowFrame.anchorMax = new Vector2(0f, 1f);
            cardWindowFrame.pivot = new Vector2(0f, 1f);
            cardWindowFrame.anchoredPosition = cardWindowOffsetFromTopLeft;
            cardWindowFrame.sizeDelta = cardWindowSize;
            cardWindowFrame.localScale = Vector3.one;
        }
        cardWindowFrame.SetAsLastSibling();

        Image frameImage = cardWindowFrame.GetComponent<Image>();
        if (frameImage == null) frameImage = cardWindowFrame.gameObject.AddComponent<Image>();
        frameImage.sprite = LoadMenuFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        bool contentAlreadyExists = menuRoot.Find("CardWindowContent") != null;
        cardWindowContentRoot = GetOrCreateRect(menuRoot, "CardWindowContent");
        if (!preserveManualCardLayout || !contentAlreadyExists)
        {
            cardWindowContentRoot.anchorMin = new Vector2(0f, 1f);
            cardWindowContentRoot.anchorMax = new Vector2(0f, 1f);
            cardWindowContentRoot.pivot = new Vector2(0f, 1f);
            cardWindowContentRoot.anchoredPosition = cardWindowOffsetFromTopLeft;
            cardWindowContentRoot.sizeDelta = cardWindowSize;
            cardWindowContentRoot.localScale = Vector3.one;
        }
        cardWindowContentRoot.SetAsLastSibling();

        bool previewAlreadyExists = menuRoot.Find("CardPreviewRoot") != null;
        cardPreviewRoot = GetOrCreateRect(menuRoot, "CardPreviewRoot");
        if (!preserveManualCardLayout || !previewAlreadyExists)
        {
            cardPreviewRoot.anchorMin = new Vector2(0f, 1f);
            cardPreviewRoot.anchorMax = new Vector2(0f, 1f);
            cardPreviewRoot.pivot = new Vector2(0f, 1f);
            cardPreviewRoot.anchoredPosition = cardPreviewOffsetFromTopLeft;
            cardPreviewRoot.sizeDelta = cardPreviewSize;
            cardPreviewRoot.localScale = Vector3.one;
        }
        cardPreviewRoot.SetAsLastSibling();

        bool previewImageAlreadyExists = cardPreviewRoot.Find("CardPreviewImage") != null;
        RectTransform previewImageRect = GetOrCreateRect(cardPreviewRoot, "CardPreviewImage");
        cardPreviewImage = previewImageRect.GetComponent<Image>();
        if (cardPreviewImage == null) cardPreviewImage = previewImageRect.gameObject.AddComponent<Image>();
        if (!preserveManualCardLayout || !previewImageAlreadyExists)
        {
            previewImageRect.anchorMin = Vector2.zero;
            previewImageRect.anchorMax = Vector2.one;
            previewImageRect.pivot = new Vector2(0.5f, 0.5f);
            previewImageRect.anchoredPosition = Vector2.zero;
            previewImageRect.sizeDelta = Vector2.zero;
            previewImageRect.localScale = Vector3.one;
        }
        cardPreviewImage.color = Color.white;
        cardPreviewImage.preserveAspect = true;
        cardPreviewImage.raycastTarget = false;
        cardPreviewView = EnsureCardPreviewView(previewImageRect);

        if (IsLegacySubWindowLayout(cardWindowFrame, cardWindowContentRoot, cardPreviewRoot, cardPreviewImage.rectTransform))
            ApplyReferenceCardWindowLayout();

        EnsureCardWindowTexts();
        SetCardWindowVisible(cardWindowOpen || (!Application.isPlaying && previewCardWindowInEditor));
    }

    private void EnsureCardWindowTexts()
    {
        if (cardWindowContentRoot == null) return;

        int entryCount = GetCardWindowEntryCount();
        TextMeshProUGUI[] entries = new TextMeshProUGUI[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            string entryName = $"CardEntry_{i}";
            bool alreadyExists = cardWindowContentRoot.Find(entryName) != null;
            entries[i] = GetOrCreateText(cardWindowContentRoot, entryName);

            StyleItemWindowText(
                entries[i],
                GetCardLabel(i),
                GetCardEntryPosition(i),
                cardWindowEntrySize,
                !preserveManualCardLayout || !alreadyExists
            );
        }

        cardWindowTexts = entries;

        bool closeAlreadyExists = cardWindowContentRoot.Find("CardClose") != null;
        cardWindowCloseText = GetOrCreateText(cardWindowContentRoot, "CardClose");
        StyleItemWindowText(
            cardWindowCloseText,
            cardCloseLabel,
            cardWindowClosePosition,
            cardWindowCloseSize,
            !preserveManualCardLayout || !closeAlreadyExists
        );

        bool pageAlreadyExists = cardWindowContentRoot.Find("CardPage") != null;
        cardWindowPageText = GetOrCreateText(cardWindowContentRoot, "CardPage");
        StyleItemWindowText(
            cardWindowPageText,
            GetCardPageText(),
            cardWindowPagePosition,
            cardWindowPageSize,
            !preserveManualCardLayout || !pageAlreadyExists
        );

        bool cursorAlreadyExists = cardWindowContentRoot.Find("CardCursor") != null;
        cardWindowCursorText = GetOrCreateText(cardWindowContentRoot, "CardCursor");
        StyleItemWindowText(
            cardWindowCursorText,
            "<",
            cardWindowListOrigin,
            new Vector2(46f, cardWindowEntrySize.y),
            !preserveManualCardLayout || !cursorAlreadyExists
        );
        cardWindowCursorText.alignment = TextAlignmentOptions.Center;
    }

    private void RefreshCardWindow()
    {
        if (!autoBuildCardWindow || cardWindowContentRoot == null) return;

        cardCurrentPage = Mathf.Clamp(cardCurrentPage, 0, GetCardPageCount() - 1);
        cardCurrentIndex = Mathf.Clamp(cardCurrentIndex, 0, GetCurrentCardLabelCount() - 1);

        bool shouldShow = cardWindowOpen || (!Application.isPlaying && previewCardWindowInEditor);
        SetCardWindowVisible(shouldShow);
        if (!shouldShow) return;

        if (cardWindowTexts != null)
        {
            List<CardMenuEntry> entries = GetCurrentCardEntries();
            for (int i = 0; i < cardWindowTexts.Length; i++)
            {
                if (cardWindowTexts[i] == null) continue;

                string label = i < entries.Count ? entries[i].label : "";
                cardWindowTexts[i].text = label;
                cardWindowTexts[i].gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }

        if (cardWindowCloseText != null)
            cardWindowCloseText.text = string.IsNullOrEmpty(cardCloseLabel) ? "close" : cardCloseLabel;

        if (cardWindowPageText != null)
            cardWindowPageText.text = GetCardPageText();

        MoveCardWindowCursor();
        RefreshCardPreview();
    }

    private void MoveCardWindowCursor()
    {
        if (cardWindowCursorText == null)
            return;

        bool shouldShow = cardWindowOpen || (!Application.isPlaying && previewCardWindowInEditor);
        cardWindowCursorText.gameObject.SetActive(shouldShow);
        if (!shouldShow) return;

        TextMeshProUGUI targetText = GetCardFocusText();

        if (targetText == null)
            return;

        float textWidth = targetText.GetPreferredValues(targetText.text).x;
        Vector2 targetPosition = targetText.rectTransform.anchoredPosition;

        cardWindowCursorText.text = "<";
        cardWindowCursorText.rectTransform.anchoredPosition = new Vector2(
            targetPosition.x + textWidth + cursorGapAfterText,
            targetPosition.y + 1f
        );
        cardWindowCursorText.transform.SetAsLastSibling();
    }

    private TextMeshProUGUI GetCardFocusText()
    {
        if (cardFocusArea == MenuPanelFocusArea.Close)
            return cardWindowCloseText;

        if (cardFocusArea == MenuPanelFocusArea.Page)
            return cardWindowPageText;

        return GetCurrentCardText();
    }

    private void RefreshCardPreview()
    {
        if (cardPreviewRoot == null || cardPreviewImage == null) return;

        bool shouldShow = cardWindowOpen || (!Application.isPlaying && previewCardWindowInEditor);
        cardPreviewRoot.gameObject.SetActive(shouldShow);
        cardPreviewImage.gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            ClearCardPreview(cardPreviewView, cardPreviewImage);
            return;
        }

        CardMenuEntry entry = GetSelectedCardEntry();
        BindCardPreview(cardPreviewView, cardPreviewImage, entry);
    }

    private CardTemplateView EnsureCardPreviewView(RectTransform previewRect)
    {
        if (previewRect == null)
            return null;

        CardTemplateView view = previewRect.GetComponent<CardTemplateView>();
        if (view == null)
            view = previewRect.gameObject.AddComponent<CardTemplateView>();

        ApplyCardPreviewTextOverride(view);

        Image image = previewRect.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
        }

        return view;
    }

    private void ApplyCardPreviewTextOverride(CardTemplateView view)
    {
        if (view == null || !overrideCardPreviewTextSize)
            return;

        view.OverrideTextFontSizes(cardPreviewCornerFontSize, cardPreviewEffectFontSize);
    }

    private void BindCardPreview(CardTemplateView previewView, Image previewImage, CardMenuEntry entry)
    {
        BaseCardSO card = entry.card;
        if (card == null && !string.IsNullOrEmpty(entry.id) && cardDatabase != null)
            card = cardDatabase.GetById(entry.id);

        if (card == null)
        {
            ClearCardPreview(previewView, previewImage);
            return;
        }

        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.color = new Color(1f, 1f, 1f, 0f);
            previewImage.enabled = true;
            previewImage.raycastTarget = false;
        }

        if (previewView != null)
        {
            ApplyCardPreviewTextOverride(previewView);
            previewView.Bind(card);
        }
    }

    private void ClearCardPreview(CardTemplateView previewView, Image previewImage)
    {
        if (previewView != null)
            previewView.Clear();

        if (previewImage == null)
            return;

        previewImage.sprite = null;
        previewImage.color = new Color(1f, 1f, 1f, 0f);
        previewImage.enabled = false;
    }

    private TextMeshProUGUI GetCurrentCardText()
    {
        if (cardWindowTexts == null || cardWindowTexts.Length == 0) return null;

        int safeIndex = Mathf.Clamp(cardCurrentIndex, 0, cardWindowTexts.Length - 1);
        return cardWindowTexts[safeIndex];
    }

    private void SetCardWindowVisible(bool visible)
    {
        if (cardWindowFrame != null)
            cardWindowFrame.gameObject.SetActive(visible);

        if (cardWindowContentRoot != null)
            cardWindowContentRoot.gameObject.SetActive(visible);

        if (cardPreviewRoot != null)
            cardPreviewRoot.gameObject.SetActive(visible);
    }

    private void OpenDeckWindow()
    {
        if (!autoBuildDeckWindow)
        {
            Debug.Log("[MenuController] Deck selected (UI disabled)", this);
            return;
        }

        if (debugMenu) Debug.Log("[MenuController] Deck window open", this);

        itemWindowOpen = false;
        SetItemWindowVisible(false);
        cardWindowOpen = false;
        SetCardWindowVisible(false);
        statusWindowOpen = false;
        SetStatusWindowVisible(false);

        deckWindowOpen = true;
        focusMode = MenuFocusMode.Deck;
        deckFocusArea = MenuPanelFocusArea.List;
        deckCurrentIndex = Mathf.Clamp(deckCurrentIndex, 0, GetCurrentDeckLabelCount() - 1);
        deckCurrentPage = Mathf.Clamp(deckCurrentPage, 0, GetDeckPageCount() - 1);

        EnsureDeckWindowView();
        RefreshDeckWindow();
        HighlightCurrent();
    }

    private void CloseDeckWindow()
    {
        if (debugMenu) Debug.Log("[MenuController] Deck window close", this);

        deckWindowOpen = false;
        focusMode = MenuFocusMode.Main;
        deckFocusArea = MenuPanelFocusArea.List;
        SetDeckWindowVisible(false);
        HighlightCurrent();
    }

    private void NavigateDeckWindowHorizontal(int delta)
    {
        if (delta == 0) return;

        MenuPanelFocusArea nextFocus = deckFocusArea;
        if (delta > 0 && deckFocusArea == MenuPanelFocusArea.List)
            nextFocus = MenuPanelFocusArea.Close;
        else if (delta < 0 && deckFocusArea != MenuPanelFocusArea.List)
            nextFocus = MenuPanelFocusArea.List;

        if (deckFocusArea == nextFocus) return;

        deckFocusArea = nextFocus;
        if (debugMenu) Debug.Log($"[MenuController] Deck focus -> {deckFocusArea}", this);

        RefreshDeckWindow();
    }

    private void NavigateDeckWindowVertical(int delta)
    {
        if (delta == 0) return;

        if (deckFocusArea == MenuPanelFocusArea.Close)
        {
            if (delta > 0)
            {
                deckFocusArea = MenuPanelFocusArea.Page;
                if (debugMenu) Debug.Log($"[MenuController] Deck focus -> {deckFocusArea}", this);
                RefreshDeckWindow();
            }

            return;
        }

        if (deckFocusArea == MenuPanelFocusArea.Page)
        {
            if (delta < 0)
            {
                deckFocusArea = MenuPanelFocusArea.Close;
                if (debugMenu) Debug.Log($"[MenuController] Deck focus -> {deckFocusArea}", this);
                RefreshDeckWindow();
            }

            return;
        }

        int count = GetCurrentDeckLabelCount();
        if (count <= 0) return;

        deckCurrentIndex = (deckCurrentIndex + delta + count) % count;
        if (debugMenu) Debug.Log($"[MenuController] Deck navigate -> {deckCurrentIndex}", this);

        RefreshDeckWindow();
    }

    private void SubmitDeckWindow()
    {
        if (deckFocusArea == MenuPanelFocusArea.Close)
        {
            CloseDeckWindow();
            return;
        }

        if (deckFocusArea == MenuPanelFocusArea.Page)
        {
            AdvanceDeckPage();
            return;
        }

        RemoveSelectedCardFromDeck();
    }

    private void AdvanceDeckPage()
    {
        int pageCount = GetDeckPageCount();
        if (pageCount <= 1)
        {
            RefreshDeckWindow();
            return;
        }

        deckCurrentPage = (deckCurrentPage + 1) % pageCount;
        deckCurrentIndex = Mathf.Clamp(deckCurrentIndex, 0, GetCurrentDeckLabelCount() - 1);

        if (debugMenu) Debug.Log($"[MenuController] Deck page -> {deckCurrentPage + 1}/{pageCount}", this);

        RefreshDeckWindow();
    }

    private void RemoveSelectedCardFromDeck()
    {
        CardMenuEntry entry = GetSelectedDeckEntry();
        if (string.IsNullOrEmpty(entry.id))
            return;

        CardStateRuntime runtime = EnsureCardStateRuntimeForMenu();
        if (runtime == null)
        {
            Debug.LogWarning("[MenuController] CardStateRuntime is missing. Cannot remove card from deck.", this);
            return;
        }

        if (!runtime.RemoveFromDeck(entry.id))
        {
            RefreshDeckWindow();
            return;
        }

        runtime.SaveNow();
        deckCurrentPage = Mathf.Clamp(deckCurrentPage, 0, GetDeckPageCount() - 1);
        deckCurrentIndex = Mathf.Clamp(deckCurrentIndex, 0, GetCurrentDeckLabelCount() - 1);

        if (debugMenu) Debug.Log($"[MenuController] Removed card from deck: {entry.id}", this);

        RefreshDeckWindow();
        RefreshCardWindow();
    }

    private void EnsureDeckWindowView()
    {
        if (!autoBuildDeckWindow || menuUI == null) return;

        RectTransform menuRoot = menuUI.transform as RectTransform;
        if (menuRoot == null) return;

        bool frameAlreadyExists = menuRoot.Find("DeckWindowFrame") != null;
        deckWindowFrame = GetOrCreateRect(menuRoot, "DeckWindowFrame");
        if (!preserveManualDeckLayout || !frameAlreadyExists)
            CopyRectFromSourceOrDefault(deckWindowFrame, cardWindowFrame, cardWindowOffsetFromTopLeft, cardWindowSize);
        deckWindowFrame.SetAsLastSibling();

        Image frameImage = deckWindowFrame.GetComponent<Image>();
        if (frameImage == null) frameImage = deckWindowFrame.gameObject.AddComponent<Image>();
        frameImage.sprite = LoadMenuFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        bool contentAlreadyExists = menuRoot.Find("DeckWindowContent") != null;
        deckWindowContentRoot = GetOrCreateRect(menuRoot, "DeckWindowContent");
        if (!preserveManualDeckLayout || !contentAlreadyExists)
            CopyRectFromSourceOrDefault(deckWindowContentRoot, cardWindowContentRoot, cardWindowOffsetFromTopLeft, cardWindowSize);
        deckWindowContentRoot.SetAsLastSibling();

        bool previewAlreadyExists = menuRoot.Find("DeckPreviewRoot") != null;
        deckPreviewRoot = GetOrCreateRect(menuRoot, "DeckPreviewRoot");
        if (!preserveManualDeckLayout || !previewAlreadyExists)
            CopyRectFromSourceOrDefault(deckPreviewRoot, cardPreviewRoot, cardPreviewOffsetFromTopLeft, cardPreviewSize);
        deckPreviewRoot.SetAsLastSibling();

        bool previewImageAlreadyExists = deckPreviewRoot.Find("DeckPreviewImage") != null;
        RectTransform previewImageRect = GetOrCreateRect(deckPreviewRoot, "DeckPreviewImage");
        deckPreviewImage = previewImageRect.GetComponent<Image>();
        if (deckPreviewImage == null) deckPreviewImage = previewImageRect.gameObject.AddComponent<Image>();
        if (!preserveManualDeckLayout || !previewImageAlreadyExists)
        {
            RectTransform sourceImage = cardPreviewImage != null ? cardPreviewImage.rectTransform : null;
            CopyRectFromSourceOrDefault(previewImageRect, sourceImage, Vector2.zero, Vector2.zero);
            if (sourceImage == null)
            {
                previewImageRect.anchorMin = Vector2.zero;
                previewImageRect.anchorMax = Vector2.one;
                previewImageRect.pivot = new Vector2(0.5f, 0.5f);
                previewImageRect.sizeDelta = Vector2.zero;
            }
        }
        deckPreviewImage.color = Color.white;
        deckPreviewImage.preserveAspect = true;
        deckPreviewImage.raycastTarget = false;
        deckPreviewView = EnsureCardPreviewView(previewImageRect);

        if (IsLegacySubWindowLayout(deckWindowFrame, deckWindowContentRoot, deckPreviewRoot, deckPreviewImage.rectTransform))
            ApplyReferenceDeckWindowLayout();

        EnsureDeckWindowTexts();
        SetDeckWindowVisible(deckWindowOpen || (!Application.isPlaying && previewDeckWindowInEditor));
    }

    private void EnsureDeckWindowTexts()
    {
        if (deckWindowContentRoot == null) return;

        int entryCount = GetDeckWindowEntryCount();
        TextMeshProUGUI[] entries = new TextMeshProUGUI[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            string entryName = $"DeckEntry_{i}";
            bool alreadyExists = deckWindowContentRoot.Find(entryName) != null;
            bool applyLayout = !preserveManualDeckLayout || !alreadyExists;
            entries[i] = GetOrCreateText(deckWindowContentRoot, entryName);

            StyleItemWindowText(
                entries[i],
                GetDeckLabel(i),
                GetDeckEntryPosition(i),
                cardWindowEntrySize,
                applyLayout
            );

            if (applyLayout)
                CopyTextLayoutFromCard(entries[i], $"CardEntry_{i}");
        }

        deckWindowTexts = entries;

        bool closeAlreadyExists = deckWindowContentRoot.Find("DeckClose") != null;
        bool applyCloseLayout = !preserveManualDeckLayout || !closeAlreadyExists;
        deckWindowCloseText = GetOrCreateText(deckWindowContentRoot, "DeckClose");
        StyleItemWindowText(
            deckWindowCloseText,
            deckCloseLabel,
            cardWindowClosePosition,
            cardWindowCloseSize,
            applyCloseLayout
        );
        if (applyCloseLayout)
            CopyTextLayoutFromCard(deckWindowCloseText, "CardClose");

        bool pageAlreadyExists = deckWindowContentRoot.Find("DeckPage") != null;
        bool applyPageLayout = !preserveManualDeckLayout || !pageAlreadyExists;
        deckWindowPageText = GetOrCreateText(deckWindowContentRoot, "DeckPage");
        StyleItemWindowText(
            deckWindowPageText,
            GetDeckPageText(),
            cardWindowPagePosition,
            cardWindowPageSize,
            applyPageLayout
        );
        if (applyPageLayout)
            CopyTextLayoutFromCard(deckWindowPageText, "CardPage");

        bool cursorAlreadyExists = deckWindowContentRoot.Find("DeckCursor") != null;
        bool applyCursorLayout = !preserveManualDeckLayout || !cursorAlreadyExists;
        deckWindowCursorText = GetOrCreateText(deckWindowContentRoot, "DeckCursor");
        StyleItemWindowText(
            deckWindowCursorText,
            "<",
            cardWindowListOrigin,
            new Vector2(46f, cardWindowEntrySize.y),
            applyCursorLayout
        );
        if (applyCursorLayout)
            CopyTextLayoutFromCard(deckWindowCursorText, "CardCursor");
        deckWindowCursorText.alignment = TextAlignmentOptions.Center;
    }

    private void RefreshDeckWindow()
    {
        if (!autoBuildDeckWindow || deckWindowContentRoot == null) return;

        deckCurrentPage = Mathf.Clamp(deckCurrentPage, 0, GetDeckPageCount() - 1);
        deckCurrentIndex = Mathf.Clamp(deckCurrentIndex, 0, GetCurrentDeckLabelCount() - 1);

        bool shouldShow = deckWindowOpen || (!Application.isPlaying && previewDeckWindowInEditor);
        SetDeckWindowVisible(shouldShow);
        if (!shouldShow) return;

        if (deckWindowTexts != null)
        {
            List<CardMenuEntry> entries = GetCurrentDeckEntries();
            for (int i = 0; i < deckWindowTexts.Length; i++)
            {
                if (deckWindowTexts[i] == null) continue;

                string label = i < entries.Count ? entries[i].label : "";
                deckWindowTexts[i].text = label;
                deckWindowTexts[i].gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }

        if (deckWindowCloseText != null)
            deckWindowCloseText.text = string.IsNullOrEmpty(deckCloseLabel) ? "close" : deckCloseLabel;

        if (deckWindowPageText != null)
            deckWindowPageText.text = GetDeckPageText();

        MoveDeckWindowCursor();
        RefreshDeckPreview();
    }

    private void MoveDeckWindowCursor()
    {
        if (deckWindowCursorText == null)
            return;

        bool shouldShow = deckWindowOpen || (!Application.isPlaying && previewDeckWindowInEditor);
        deckWindowCursorText.gameObject.SetActive(shouldShow);
        if (!shouldShow) return;

        TextMeshProUGUI targetText = GetDeckFocusText();
        if (targetText == null)
            return;

        float textWidth = targetText.GetPreferredValues(targetText.text).x;
        Vector2 targetPosition = targetText.rectTransform.anchoredPosition;

        deckWindowCursorText.text = "<";
        deckWindowCursorText.rectTransform.anchoredPosition = new Vector2(
            targetPosition.x + textWidth + cursorGapAfterText,
            targetPosition.y + 1f
        );
        deckWindowCursorText.transform.SetAsLastSibling();
    }

    private TextMeshProUGUI GetDeckFocusText()
    {
        if (deckFocusArea == MenuPanelFocusArea.Close)
            return deckWindowCloseText;

        if (deckFocusArea == MenuPanelFocusArea.Page)
            return deckWindowPageText;

        return GetCurrentDeckText();
    }

    private void RefreshDeckPreview()
    {
        if (deckPreviewRoot == null || deckPreviewImage == null) return;

        bool shouldShow = deckWindowOpen || (!Application.isPlaying && previewDeckWindowInEditor);
        deckPreviewRoot.gameObject.SetActive(shouldShow);
        deckPreviewImage.gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            ClearCardPreview(deckPreviewView, deckPreviewImage);
            return;
        }

        CardMenuEntry entry = GetSelectedDeckEntry();
        BindCardPreview(deckPreviewView, deckPreviewImage, entry);
    }

    private TextMeshProUGUI GetCurrentDeckText()
    {
        if (deckWindowTexts == null || deckWindowTexts.Length == 0) return null;

        int safeIndex = Mathf.Clamp(deckCurrentIndex, 0, deckWindowTexts.Length - 1);
        return deckWindowTexts[safeIndex];
    }

    private void SetDeckWindowVisible(bool visible)
    {
        if (deckWindowFrame != null)
            deckWindowFrame.gameObject.SetActive(visible);

        if (deckWindowContentRoot != null)
            deckWindowContentRoot.gameObject.SetActive(visible);

        if (deckPreviewRoot != null)
            deckPreviewRoot.gameObject.SetActive(visible);
    }

    private void OpenStatusWindow()
    {
        if (!autoBuildStatusWindow)
        {
            Debug.Log("[MenuController] Status selected (UI disabled)", this);
            return;
        }

        if (debugMenu) Debug.Log("[MenuController] Status window open", this);

        itemWindowOpen = false;
        SetItemWindowVisible(false);
        cardWindowOpen = false;
        SetCardWindowVisible(false);
        deckWindowOpen = false;
        SetDeckWindowVisible(false);

        statusWindowOpen = true;
        focusMode = MenuFocusMode.Status;

        EnsureStatusWindowView();
        RefreshStatusWindow();
        HighlightCurrent();
    }

    private void CloseStatusWindow()
    {
        if (debugMenu) Debug.Log("[MenuController] Status window close", this);

        statusWindowOpen = false;
        focusMode = MenuFocusMode.Main;
        SetStatusWindowVisible(false);
        HighlightCurrent();
    }

    private void NavigateStatusWindowHorizontal(int delta)
    {
        if (delta != 0)
            RefreshStatusWindow();
    }

    private void NavigateStatusWindowVertical(int delta)
    {
        if (delta != 0)
            RefreshStatusWindow();
    }

    private void SubmitStatusWindow()
    {
        CloseStatusWindow();
    }

    private void EnsureStatusWindowView()
    {
        if (!autoBuildStatusWindow || menuUI == null) return;

        RectTransform menuRoot = menuUI.transform as RectTransform;
        if (menuRoot == null) return;

        bool frameAlreadyExists = menuRoot.Find("StatusWindowFrame") != null;
        statusWindowFrame = GetOrCreateRect(menuRoot, "StatusWindowFrame");
        if (!preserveManualStatusLayout || !frameAlreadyExists)
            CopyRectFromSourceOrDefault(statusWindowFrame, cardWindowFrame, statusWindowOffsetFromTopLeft, statusWindowSize);
        statusWindowFrame.SetAsLastSibling();

        Image frameImage = statusWindowFrame.GetComponent<Image>();
        if (frameImage == null) frameImage = statusWindowFrame.gameObject.AddComponent<Image>();
        frameImage.sprite = LoadMenuFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        bool contentAlreadyExists = menuRoot.Find("StatusWindowContent") != null;
        statusWindowContentRoot = GetOrCreateRect(menuRoot, "StatusWindowContent");
        if (!preserveManualStatusLayout || !contentAlreadyExists)
            CopyRectFromSourceOrDefault(statusWindowContentRoot, cardWindowContentRoot, statusWindowOffsetFromTopLeft, statusWindowSize);
        statusWindowContentRoot.SetAsLastSibling();

        if (IsLegacySubWindowLayout(statusWindowFrame, statusWindowContentRoot, null, null))
            ApplyReferenceStatusWindowLayout();

        EnsureStatusWindowTexts();
        SetStatusWindowVisible(statusWindowOpen || (!Application.isPlaying && previewStatusWindowInEditor));
    }

    private void EnsureStatusWindowTexts()
    {
        if (statusWindowContentRoot == null) return;

        string[] labels = GetStatusLabels();
        TextMeshProUGUI[] entries = new TextMeshProUGUI[labels.Length];

        for (int i = 0; i < entries.Length; i++)
        {
            string entryName = $"StatusEntry_{i}";
            bool alreadyExists = statusWindowContentRoot.Find(entryName) != null;
            entries[i] = GetOrCreateText(statusWindowContentRoot, entryName);

            StyleItemWindowText(
                entries[i],
                labels[i],
                GetStatusEntryPosition(i),
                statusWindowEntrySize,
                !preserveManualStatusLayout || !alreadyExists
            );
        }

        statusWindowTexts = entries;

        bool closeAlreadyExists = statusWindowContentRoot.Find("StatusClose") != null;
        statusWindowCloseText = GetOrCreateText(statusWindowContentRoot, "StatusClose");
        StyleItemWindowText(
            statusWindowCloseText,
            statusCloseLabel,
            statusWindowClosePosition,
            statusWindowCloseSize,
            !preserveManualStatusLayout || !closeAlreadyExists
        );

        bool cursorAlreadyExists = statusWindowContentRoot.Find("StatusCursor") != null;
        statusWindowCursorText = GetOrCreateText(statusWindowContentRoot, "StatusCursor");
        StyleItemWindowText(
            statusWindowCursorText,
            "<",
            statusWindowClosePosition,
            new Vector2(46f, statusWindowCloseSize.y),
            !preserveManualStatusLayout || !cursorAlreadyExists
        );
        statusWindowCursorText.alignment = TextAlignmentOptions.Center;
    }

    private void RefreshStatusWindow()
    {
        if (!autoBuildStatusWindow || statusWindowContentRoot == null) return;

        bool shouldShow = statusWindowOpen || (!Application.isPlaying && previewStatusWindowInEditor);
        SetStatusWindowVisible(shouldShow);
        if (!shouldShow) return;

        string[] labels = GetStatusLabels();
        if (statusWindowTexts != null)
        {
            for (int i = 0; i < statusWindowTexts.Length; i++)
            {
                if (statusWindowTexts[i] == null) continue;

                string label = i < labels.Length ? labels[i] : "";
                statusWindowTexts[i].text = label;
                statusWindowTexts[i].gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }

        if (statusWindowCloseText != null)
            statusWindowCloseText.text = string.IsNullOrEmpty(statusCloseLabel) ? "close" : statusCloseLabel;

        MoveStatusWindowCursor();
    }

    private void MoveStatusWindowCursor()
    {
        if (statusWindowCursorText == null)
            return;

        bool shouldShow = statusWindowOpen || (!Application.isPlaying && previewStatusWindowInEditor);
        statusWindowCursorText.gameObject.SetActive(shouldShow);
        if (!shouldShow || statusWindowCloseText == null) return;

        float textWidth = statusWindowCloseText.GetPreferredValues(statusWindowCloseText.text).x;
        Vector2 targetPosition = statusWindowCloseText.rectTransform.anchoredPosition;

        statusWindowCursorText.text = "<";
        statusWindowCursorText.rectTransform.anchoredPosition = new Vector2(
            targetPosition.x + textWidth + cursorGapAfterText,
            targetPosition.y + 1f
        );
        statusWindowCursorText.transform.SetAsLastSibling();
    }

    private void SetStatusWindowVisible(bool visible)
    {
        if (statusWindowFrame != null)
            statusWindowFrame.gameObject.SetActive(visible);

        if (statusWindowContentRoot != null)
            statusWindowContentRoot.gameObject.SetActive(visible);
    }

    private Vector2 GetStatusEntryPosition(int index)
    {
        return new Vector2(
            statusWindowListOrigin.x,
            statusWindowListOrigin.y - index * statusWindowRowSpacing
        );
    }

    private string[] GetStatusLabels()
    {
        PlayerData player = GetPlayerDataForStatus();
        if (player == null)
            return new[] { string.IsNullOrEmpty(emptyStatusLabel) ? "no data" : emptyStatusLabel };

        return new[]
        {
            $"name : {player.playerName}",
            $"hp : {Mathf.Max(0, player.currentHP)}/{Mathf.Max(1, player.maxHP)}",
            $"attack : {Mathf.Max(0, player.attack)}",
            $"defense : {Mathf.Max(0, player.defense)}",
            $"speed : {Mathf.Max(0, player.speed)}"
        };
    }

    private PlayerData GetPlayerDataForStatus()
    {
        if (PlayerDataRuntime.Instance != null && PlayerDataRuntime.Instance.Data != null)
            return PlayerDataRuntime.Instance.Data;

        PlayerData saved = PlayerDataStore.Load();
        if (saved != null)
            return saved;

        PlayerData fallback = new PlayerData();
        fallback.InitDefaults();
        return fallback;
    }

    private Vector2 GetCardEntryPosition(int index)
    {
        return new Vector2(
            cardWindowListOrigin.x,
            cardWindowListOrigin.y - index * cardWindowRowSpacing
        );
    }

    private string GetCardLabel(int index)
    {
        List<CardMenuEntry> entries = GetCurrentCardEntries();
        return index >= 0 && index < entries.Count ? entries[index].label : "";
    }

    private List<CardMenuEntry> GetCurrentCardEntries()
    {
        List<CardMenuEntry> allEntries = GetAllCardWindowEntries();
        int pageSize = GetCardWindowEntryCount();
        int startIndex = Mathf.Clamp(cardCurrentPage, 0, GetCardPageCount() - 1) * pageSize;
        int count = Mathf.Clamp(allEntries.Count - startIndex, 0, pageSize);
        List<CardMenuEntry> pageEntries = new List<CardMenuEntry>(count);

        for (int i = 0; i < count; i++)
            pageEntries.Add(allEntries[startIndex + i]);

        if (pageEntries.Count == 0)
            pageEntries.Add(GetEmptyCardEntry());

        return pageEntries;
    }

    private CardMenuEntry GetSelectedCardEntry()
    {
        List<CardMenuEntry> entries = GetCurrentCardEntries();
        int safeIndex = Mathf.Clamp(cardCurrentIndex, 0, entries.Count - 1);
        return entries[safeIndex];
    }

    private int GetCurrentCardLabelCount()
    {
        return Mathf.Max(1, GetCurrentCardEntries().Count);
    }

    private int GetCardWindowEntryCount()
    {
        return Mathf.Max(1, cardEntriesPerPage);
    }

    private int GetCardPageCount()
    {
        int labelCount = GetAllCardWindowEntries().Count;
        return Mathf.Max(1, Mathf.CeilToInt(labelCount / (float)GetCardWindowEntryCount()));
    }

    private string GetCardPageText()
    {
        return $"{cardCurrentPage + 1}/{GetCardPageCount()}";
    }

    private List<CardMenuEntry> GetAllCardWindowEntries()
    {
        ResolveCardDatabase();

        List<CardMenuEntry> entries = new List<CardMenuEntry>();
        List<string> ownedCardIds = new List<string>();
        HashSet<string> deckIds = GetDeckIdSet();

        if (TryGetOwnedCardIds(ownedCardIds))
        {
            HashSet<string> seenIds = new HashSet<string>();
            for (int i = 0; i < ownedCardIds.Count; i++)
            {
                string id = ownedCardIds[i];
                if (string.IsNullOrEmpty(id) || !seenIds.Add(id)) continue;
                if (deckIds.Contains(id)) continue;
                entries.Add(CreateCardEntry(id, cardDatabase != null ? cardDatabase.GetById(id) : null));
            }
        }
        else if (cardDatabase != null && cardDatabase.cards != null)
        {
            HashSet<string> seenIds = new HashSet<string>();
            for (int i = 0; i < cardDatabase.cards.Count; i++)
            {
                BaseCardSO card = cardDatabase.cards[i];
                if (card == null) continue;

                string id = string.IsNullOrEmpty(card.id) ? card.name : card.id;
                if (string.IsNullOrEmpty(id) || !seenIds.Add(id)) continue;
                entries.Add(CreateCardEntry(id, card));
            }
        }

        if (entries.Count == 0)
            entries.Add(GetEmptyCardEntry());

        return entries;
    }

    private bool TryGetOwnedCardIds(List<string> ids)
    {
        CardSaveData data = GetCardSaveDataForMenu();

        if (data == null || data.owned == null)
            return false;

        for (int i = 0; i < data.owned.Count; i++)
        {
            if (!string.IsNullOrEmpty(data.owned[i]))
                ids.Add(data.owned[i]);
        }

        return true;
    }

    private HashSet<string> GetDeckIdSet()
    {
        List<string> deckIds = new List<string>();
        if (!TryGetDeckCardIds(deckIds))
            return new HashSet<string>();

        return new HashSet<string>(deckIds);
    }

    private bool TryGetDeckCardIds(List<string> ids)
    {
        CardSaveData data = GetCardSaveDataForMenu();

        if (data == null || data.deck == null)
            return false;

        for (int i = 0; i < data.deck.Count; i++)
        {
            if (!string.IsNullOrEmpty(data.deck[i]))
                ids.Add(data.deck[i]);
        }

        return true;
    }

    private CardSaveData GetCardSaveDataForMenu()
    {
        CardStateRuntime runtime = Application.isPlaying
            ? EnsureCardStateRuntimeForMenu()
            : CardStateRuntime.Instance;

        return runtime != null ? runtime.Data : null;
    }

    private CardStateRuntime EnsureCardStateRuntimeForMenu()
    {
        if (CardStateRuntime.Instance != null)
            return CardStateRuntime.Instance;

        if (!Application.isPlaying)
            return null;

        GameObject runtimeObject = new GameObject("CardStateRuntime (Menu Auto)");
        return runtimeObject.AddComponent<CardStateRuntime>();
    }

    private CardMenuEntry CreateCardEntry(string id, BaseCardSO card)
    {
        string label = id;
        if (card != null && !string.IsNullOrEmpty(card.displayName))
            label = card.displayName;

        return new CardMenuEntry
        {
            id = id,
            card = card,
            label = string.IsNullOrEmpty(label) ? GetEmptyCardLabel() : label
        };
    }

    private CardMenuEntry GetEmptyCardEntry()
    {
        return new CardMenuEntry
        {
            id = "",
            card = null,
            label = GetEmptyCardLabel()
        };
    }

    private string GetEmptyCardLabel()
    {
        return string.IsNullOrEmpty(emptyCardLabel) ? "empty" : emptyCardLabel;
    }

    private Vector2 GetDeckEntryPosition(int index)
    {
        return new Vector2(
            cardWindowListOrigin.x,
            cardWindowListOrigin.y - index * cardWindowRowSpacing
        );
    }

    private string GetDeckLabel(int index)
    {
        List<CardMenuEntry> entries = GetCurrentDeckEntries();
        return index >= 0 && index < entries.Count ? entries[index].label : "";
    }

    private List<CardMenuEntry> GetCurrentDeckEntries()
    {
        List<CardMenuEntry> allEntries = GetAllDeckWindowEntries();
        int pageSize = GetDeckWindowEntryCount();
        int startIndex = Mathf.Clamp(deckCurrentPage, 0, GetDeckPageCount() - 1) * pageSize;
        int count = Mathf.Clamp(allEntries.Count - startIndex, 0, pageSize);
        List<CardMenuEntry> pageEntries = new List<CardMenuEntry>(count);

        for (int i = 0; i < count; i++)
            pageEntries.Add(allEntries[startIndex + i]);

        if (pageEntries.Count == 0)
            pageEntries.Add(GetEmptyDeckEntry());

        return pageEntries;
    }

    private CardMenuEntry GetSelectedDeckEntry()
    {
        List<CardMenuEntry> entries = GetCurrentDeckEntries();
        int safeIndex = Mathf.Clamp(deckCurrentIndex, 0, entries.Count - 1);
        return entries[safeIndex];
    }

    private int GetCurrentDeckLabelCount()
    {
        return Mathf.Max(1, GetCurrentDeckEntries().Count);
    }

    private int GetDeckWindowEntryCount()
    {
        return GetCardWindowEntryCount();
    }

    private int GetDeckPageCount()
    {
        int labelCount = GetAllDeckWindowEntries().Count;
        return Mathf.Max(1, Mathf.CeilToInt(labelCount / (float)GetDeckWindowEntryCount()));
    }

    private string GetDeckPageText()
    {
        return $"{deckCurrentPage + 1}/{GetDeckPageCount()}";
    }

    private List<CardMenuEntry> GetAllDeckWindowEntries()
    {
        ResolveCardDatabase();

        List<CardMenuEntry> entries = new List<CardMenuEntry>();
        List<string> deckCardIds = new List<string>();

        if (TryGetDeckCardIds(deckCardIds))
        {
            HashSet<string> seenIds = new HashSet<string>();
            for (int i = 0; i < deckCardIds.Count; i++)
            {
                string id = deckCardIds[i];
                if (string.IsNullOrEmpty(id) || !seenIds.Add(id)) continue;
                entries.Add(CreateCardEntry(id, cardDatabase != null ? cardDatabase.GetById(id) : null));
            }
        }

        if (entries.Count == 0)
            entries.Add(GetEmptyDeckEntry());

        return entries;
    }

    private CardMenuEntry GetEmptyDeckEntry()
    {
        return new CardMenuEntry
        {
            id = "",
            card = null,
            label = string.IsNullOrEmpty(emptyDeckLabel) ? "empty" : emptyDeckLabel
        };
    }

    private Vector2 GetItemEntryPosition(int index)
    {
        return new Vector2(
            itemWindowListOrigin.x,
            itemWindowListOrigin.y - index * itemWindowRowSpacing
        );
    }

    private string GetItemLabel(int index)
    {
        List<ItemMenuEntry> entries = GetCurrentItemEntries();
        return index >= 0 && index < entries.Count ? entries[index].label : "";
    }

    private string[] GetCurrentItemLabels()
    {
        List<ItemMenuEntry> entries = GetCurrentItemEntries();
        string[] labels = new string[entries.Count];

        for (int i = 0; i < entries.Count; i++)
            labels[i] = entries[i].label;

        return labels;
    }

    private int GetCurrentItemLabelCount()
    {
        return Mathf.Max(1, GetCurrentItemEntries().Count);
    }

    private int GetItemWindowEntryCount()
    {
        return Mathf.Max(1, itemEntriesPerPage);
    }

    private int GetItemPageCount()
    {
        int labelCount = GetAllItemWindowEntries().Count;
        return Mathf.Max(1, Mathf.CeilToInt(labelCount / (float)GetItemWindowEntryCount()));
    }

    private string GetItemPageText()
    {
        return $"{itemCurrentPage + 1}/{GetItemPageCount()}";
    }

    private List<ItemMenuEntry> GetCurrentItemEntries()
    {
        List<ItemMenuEntry> allEntries = GetAllItemWindowEntries();
        int pageSize = GetItemWindowEntryCount();
        int startIndex = Mathf.Clamp(itemCurrentPage, 0, GetItemPageCount() - 1) * pageSize;
        int count = Mathf.Clamp(allEntries.Count - startIndex, 0, pageSize);
        List<ItemMenuEntry> pageEntries = new List<ItemMenuEntry>(count);

        for (int i = 0; i < count; i++)
            pageEntries.Add(allEntries[startIndex + i]);

        if (pageEntries.Count == 0)
            pageEntries.Add(GetEmptyItemEntry());

        return pageEntries;
    }

    private ItemMenuEntry GetSelectedItemEntry()
    {
        List<ItemMenuEntry> entries = GetCurrentItemEntries();
        int safeIndex = Mathf.Clamp(itemCurrentIndex, 0, entries.Count - 1);
        return entries[safeIndex];
    }

    private List<ItemMenuEntry> GetAllItemWindowEntries()
    {
        List<ItemMenuEntry> entries = new List<ItemMenuEntry>();

        if (TryAppendInventoryItemEntries(entries))
        {
            if (entries.Count == 0)
                entries.Add(GetEmptyItemEntry());

            return entries;
        }

        AppendFallbackItemEntries(entries, itemPageOneLabels);
        AppendFallbackItemEntries(entries, itemPageTwoLabels);

        if (entries.Count == 0)
            entries.Add(GetEmptyItemEntry());

        return entries;
    }

    private bool TryAppendInventoryItemEntries(List<ItemMenuEntry> entries)
    {
        if (!TryGetInventoryData(out InventorySaveData inventoryData))
            return false;

        ResolveItemDatabase();

        for (int i = 0; i < inventoryData.items.Length; i++)
        {
            InventoryItemEntry entry = inventoryData.items[i];
            if (entry == null || entry.quantity <= 0)
                continue;

            ItemSO itemDefinition = itemDatabase != null ? itemDatabase.GetById(entry.id) : null;
            entries.Add(CreateItemEntry(entry.id, itemDefinition, entry.quantity));
        }

        return true;
    }

    private bool TryGetInventoryData(out InventorySaveData inventoryData)
    {
        inventoryData = null;

        if (Application.isPlaying)
        {
            ItemRuntime runtime = EnsureItemRuntimeForMenu();
            if (runtime != null && runtime.CurrentData != null && runtime.CurrentData.items != null)
            {
                inventoryData = runtime.CurrentData;
                return true;
            }
        }

        if (!useDefaultInventoryJsonWhenRuntimeMissing || string.IsNullOrEmpty(defaultInventoryJsonName))
            return false;

        TextAsset json = Resources.Load<TextAsset>(defaultInventoryJsonName);
        if (json == null)
            return false;

        inventoryData = JsonUtility.FromJson<InventorySaveData>(json.text);
        return inventoryData != null && inventoryData.items != null;
    }

    private ItemRuntime EnsureItemRuntimeForMenu()
    {
        if (ItemRuntime.Instance != null)
            return ItemRuntime.Instance;

        if (!Application.isPlaying)
            return null;

        GameObject runtimeObject = new GameObject("ItemRuntime (Menu Auto)");
        return runtimeObject.AddComponent<ItemRuntime>();
    }

    private ItemMenuEntry CreateItemEntry(string id, ItemSO item, int quantity)
    {
        string itemName = GetItemDisplayName(item, id);

        return new ItemMenuEntry
        {
            id = id,
            item = item,
            label = $"{(string.IsNullOrEmpty(itemName) ? GetEmptyItemLabel() : itemName)} x {quantity}"
        };
    }

    private string GetItemDisplayName(ItemSO item, string fallbackId)
    {
        if (item != null && !string.IsNullOrEmpty(item.displayName))
            return item.displayName;

        if (!string.IsNullOrEmpty(fallbackId))
            return fallbackId;

        return GetEmptyItemLabel();
    }

    private ItemSO ResolveItemDefinition(ItemMenuEntry entry)
    {
        if (entry.item != null)
            return entry.item;

        if (string.IsNullOrEmpty(entry.id))
            return null;

        ResolveItemDatabase();
        return itemDatabase != null ? itemDatabase.GetById(entry.id) : null;
    }

    private string GetItemEffectDescriptionText(ItemMenuEntry entry, ItemSO item)
    {
        if (item == null && string.IsNullOrEmpty(entry.id))
            return "설명";

        string description = item != null ? item.description : "";

        if (!string.IsNullOrWhiteSpace(description))
            return description;

        return "설명";
    }

    private ItemMenuEntry CreateFallbackItemEntry(string label)
    {
        return new ItemMenuEntry
        {
            id = "",
            item = null,
            label = string.IsNullOrEmpty(label) ? GetEmptyItemLabel() : label
        };
    }

    private ItemMenuEntry GetEmptyItemEntry()
    {
        return CreateFallbackItemEntry(GetEmptyItemLabel());
    }

    private void AppendFallbackItemEntries(List<ItemMenuEntry> entries, string[] source)
    {
        if (source == null) return;

        for (int i = 0; i < source.Length; i++)
        {
            if (!string.IsNullOrEmpty(source[i]))
                entries.Add(CreateFallbackItemEntry(source[i]));
        }
    }

    private string GetEmptyItemLabel()
    {
        return string.IsNullOrEmpty(emptyItemLabel) ? "empty" : emptyItemLabel;
    }

    private TMP_FontAsset GetRetroFont()
    {
        TextMeshProUGUI fontSource = GetFirstMenuText();
        if (fontSource != null && fontSource.font != null)
            return fontSource.font;

        return panelText != null ? panelText.font : null;
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

    private void ApplyRetroContentLayout()
    {
        if (retroContentRoot == null) return;

        retroContentRoot.anchorMin = new Vector2(0f, 1f);
        retroContentRoot.anchorMax = new Vector2(0f, 1f);
        retroContentRoot.pivot = new Vector2(0f, 1f);
        retroContentRoot.anchoredPosition = contentOffsetFromTopLeft;
        retroContentRoot.sizeDelta = windowSize;
        retroContentRoot.localScale = Vector3.one;
    }

    private void LayoutMenuItems(bool forceDefaultLayout = false)
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

            bool applyDefaultLayout = forceDefaultLayout || !preserveManualLayout || !itemAlreadyInContent;
            if (applyDefaultLayout)
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

            StyleMenuItemText(itemText, i, itemRoot == itemText.rectTransform, applyDefaultLayout);
        }
    }

    private bool IsRetroMenuLayoutCollapsed()
    {
        if (menuItems == null || menuItems.Length < 3 || retroContentRoot == null)
            return false;

        int itemCount = 0;
        int stackedAfterFirst = 0;
        int centeredZeroItems = 0;
        Vector2 firstPosition = Vector2.zero;
        bool hasFirstPosition = false;

        for (int i = 0; i < menuItems.Length; i++)
        {
            TextMeshProUGUI itemText = menuItems[i];
            if (itemText == null) continue;

            RectTransform itemRoot = GetItemRoot(itemText);
            if (itemRoot == null || itemRoot.parent != retroContentRoot)
                continue;

            itemCount++;

            if (!hasFirstPosition)
            {
                firstPosition = itemRoot.anchoredPosition;
                hasFirstPosition = true;
            }
            else if (Vector2.Distance(firstPosition, itemRoot.anchoredPosition) <= 0.5f)
            {
                stackedAfterFirst++;
            }

            bool centeredAnchor =
                Vector2.Distance(itemRoot.anchorMin, new Vector2(0.5f, 0.5f)) <= 0.001f &&
                Vector2.Distance(itemRoot.anchorMax, new Vector2(0.5f, 0.5f)) <= 0.001f &&
                Vector2.Distance(itemRoot.pivot, new Vector2(0.5f, 0.5f)) <= 0.001f;

            if (centeredAnchor && itemRoot.anchoredPosition.sqrMagnitude <= 1f)
                centeredZeroItems++;
        }

        return itemCount >= 3 && (stackedAfterFirst >= itemCount - 1 || centeredZeroItems >= 3);
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
        if (menuItems != null && menuItems.Length == targetCount && !HasMissingMenuItems())
            return;

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

    private bool HasMissingMenuItems()
    {
        if (menuItems == null) return true;

        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] == null)
                return true;
        }

        return false;
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
