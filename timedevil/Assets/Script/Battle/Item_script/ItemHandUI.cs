using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ItemHandUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private RectTransform itemHandRect;

    [Header("Render Order")]
    [SerializeField] private bool forceFrontCanvas = true;
    [SerializeField] private int frontSortingOrder = 80;

    private CanvasGroup cg;
    private Canvas frontCanvas;
    private Coroutine motionRoutine;
    private Vector2 defaultAnchoredPosition;
    private bool hasDefaultPosition;
    private bool itemInteractionMode;
    private bool enemyTurn;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!itemHandRect) itemHandRect = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (!itemHandRect) itemHandRect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        CaptureDefaultPosition();
        EnsureFrontCanvas();
        Hide();
    }

    void OnEnable()
    {
        if (menu)
        {
            menu.onFocusChanged.AddListener(OnMenuFocusChanged);
            OnMenuFocusChanged(menu.Index);
        }
    }

    void OnDisable()
    {
        if (menu)
            menu.onFocusChanged.RemoveListener(OnMenuFocusChanged);

        if (motionRoutine != null)
        {
            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }
    }

    private void OnMenuFocusChanged(int idx)
    {
        if (itemInteractionMode)
            return;

        if (enemyTurn)
        {
            Hide();
            return;
        }

        if (idx == ResolveItemIndex()) Show();
        else Hide();
    }

    public void SetEnemyTurn(bool on)
    {
        enemyTurn = on;
        if (on)
        {
            itemInteractionMode = false;
            Hide();
        }
        else if (menu)
        {
            OnMenuFocusChanged(menu.Index);
        }
        else
        {
            Hide();
        }
    }

    public void ShowForItemInteraction(float shownY, float hiddenY, float duration, AnimationCurve ease)
    {
        if (!itemHandRect) itemHandRect = GetComponent<RectTransform>();
        if (!cg) cg = GetComponent<CanvasGroup>();
        CaptureDefaultPosition();
        BringToFront();

        itemInteractionMode = true;
        SetVisible(true);

        if (!itemHandRect)
            return;

        Vector2 current = itemHandRect.anchoredPosition;
        Vector2 from = new Vector2(current.x, hiddenY);
        Vector2 to = new Vector2(current.x, shownY);
        PlayMove(from, to, duration, ease);
    }

    public void ExitItemInteractionMode(float duration, AnimationCurve ease)
    {
        itemInteractionMode = false;

        if (!itemHandRect)
            itemHandRect = GetComponent<RectTransform>();

        bool shouldShowByFocus = !enemyTurn && menu && menu.Index == ResolveItemIndex();
        if (shouldShowByFocus)
        {
            SetVisible(true);
            if (itemHandRect && hasDefaultPosition)
                PlayMove(itemHandRect.anchoredPosition, defaultAnchoredPosition, duration, ease);
            return;
        }

        if (motionRoutine != null)
        {
            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }

        if (itemHandRect && hasDefaultPosition)
            itemHandRect.anchoredPosition = defaultAnchoredPosition;

        Hide();
    }

    private void Show()
    {
        CaptureDefaultPosition();
        if (itemHandRect && hasDefaultPosition)
            itemHandRect.anchoredPosition = defaultAnchoredPosition;

        BringToFront();
        SetVisible(true);
    }

    private int ResolveItemIndex()
    {
        if (menu == null)
            return -1;

        for (int i = 0; i < menu.EntryCount; i++)
        {
            GameObject entry = menu.GetEntryObject(i);
            if (entry && entry.name.ToLowerInvariant().Contains("item"))
                return i;
        }

        return menu.EntryCount >= 5 ? 1 : -1;
    }

    private void Hide()
    {
        if (motionRoutine != null)
        {
            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }

        if (itemHandRect && hasDefaultPosition)
            itemHandRect.anchoredPosition = defaultAnchoredPosition;

        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        if (!cg) return;

        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
        gameObject.SetActive(true);
    }

    private void CaptureDefaultPosition()
    {
        if (hasDefaultPosition)
            return;

        if (!itemHandRect)
            itemHandRect = GetComponent<RectTransform>();

        if (!itemHandRect)
            return;

        defaultAnchoredPosition = itemHandRect.anchoredPosition;
        hasDefaultPosition = true;
    }

    private void EnsureFrontCanvas()
    {
        if (!forceFrontCanvas)
            return;

        if (!frontCanvas)
            frontCanvas = GetComponent<Canvas>();

        if (!frontCanvas)
            frontCanvas = gameObject.AddComponent<Canvas>();

        frontCanvas.overrideSorting = true;
        frontCanvas.sortingOrder = frontSortingOrder;

        if (!GetComponent<GraphicRaycaster>())
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void BringToFront()
    {
        EnsureFrontCanvas();
        transform.SetAsLastSibling();
    }

    private void PlayMove(Vector2 from, Vector2 to, float duration, AnimationCurve ease)
    {
        if (!itemHandRect)
            return;

        if (motionRoutine != null)
            StopCoroutine(motionRoutine);

        if (!Application.isPlaying || duration <= 0f)
        {
            itemHandRect.anchoredPosition = to;
            return;
        }

        itemHandRect.anchoredPosition = from;
        motionRoutine = StartCoroutine(Co_Move(from, to, duration, ease));
    }

    private IEnumerator Co_Move(Vector2 from, Vector2 to, float duration, AnimationCurve ease)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < safeDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / safeDuration);
            float eased = ease != null ? ease.Evaluate(u) : u;
            if (itemHandRect)
                itemHandRect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        if (itemHandRect)
            itemHandRect.anchoredPosition = to;
        motionRoutine = null;
    }
}
