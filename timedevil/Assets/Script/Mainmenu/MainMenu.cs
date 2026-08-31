// Assets/Script/Mainmenu/MainMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public AudioSource sfxPlayer;
    public AudioClip clickSound;

    [Header("Target Scene")]
    public string myRoomSceneName = "Myroom";

    [Header("New Game Intro")]
    [SerializeField] private MainMenuNewGameIntro newGameIntro;

    [Header("Pre Intro Black Fade")]
    [SerializeField] private bool usePreIntroBlackFade = true;
    [SerializeField, Min(0f)] private float preIntroFadeToBlackSeconds = 0.85f;
    [SerializeField, Min(0f)] private float preIntroBlackHoldSeconds = 0.15f;
    [SerializeField, Min(0f)] private float preIntroFadeFromBlackSeconds = 0.45f;
    [SerializeField] private Canvas preIntroFadeCanvas;
    [SerializeField] private CanvasGroup preIntroFadeGroup;
    [SerializeField] private bool autoCreatePreIntroFadeIfMissing = true;
    [SerializeField] private int preIntroFadeSortingOrder = 2000;

    [Header("Return From Game Fade")]
    [SerializeField] private bool useReturnFromGameFade = true;
    [SerializeField, Min(0f)] private float returnFadeFromBlackSeconds = 0.45f;
    [SerializeField] private Canvas returnFadeCanvas;
    [SerializeField] private CanvasGroup returnFadeGroup;
    [SerializeField] private bool autoCreateReturnFadeIfMissing = true;
    [SerializeField] private int returnFadeSortingOrder = 3000;

    private bool _newGameStarting = false;
    private Coroutine _newGameRoutine;
    private Coroutine _returnFadeRoutine;

    private static bool s_returnFadeInRequested;
    private static float s_requestedReturnFadeSeconds = -1f;

    public static void RequestReturnFadeIn(float fadeFromBlackSeconds = -1f)
    {
        s_returnFadeInRequested = true;
        s_requestedReturnFadeSeconds = fadeFromBlackSeconds;
    }

    private void Awake()
    {
        if (newGameIntro == null)
            newGameIntro = GetComponent<MainMenuNewGameIntro>();

        if (preIntroFadeGroup != null)
            SetPreIntroFadeAlpha(0f, false);

        if (s_returnFadeInRequested && useReturnFromGameFade && ResolveReturnFadeUi())
        {
            BringReturnFadeToFront();
            SetReturnFadeAlpha(1f, true);
        }
        else if (returnFadeGroup != null)
        {
            SetReturnFadeAlpha(0f, false);
        }
    }

    private void Start()
    {
        if (!s_returnFadeInRequested)
            return;

        float fadeSeconds = s_requestedReturnFadeSeconds >= 0f
            ? s_requestedReturnFadeSeconds
            : returnFadeFromBlackSeconds;

        s_returnFadeInRequested = false;
        s_requestedReturnFadeSeconds = -1f;

        if (!useReturnFromGameFade || !ResolveReturnFadeUi())
            return;

        BringReturnFadeToFront();
        _returnFadeRoutine = StartCoroutine(CoPlayReturnFadeIn(fadeSeconds));
    }

    // 버튼: 새 게임
    public void NewGame()
    {
        if (_newGameStarting)
            return;

        PlayClick();

        _newGameStarting = true;
        _newGameRoutine = StartCoroutine(CoStartNewGame());
    }

    private IEnumerator CoStartNewGame()
    {
        if (newGameIntro != null && newGameIntro.HasPlayableIntro)
        {
            yield return CoPlayPreIntroFadeToBlack();
            newGameIntro.Play(CompleteNewGame);
            yield return CoFadePreIntroOverlayOut();
            yield break;
        }

        yield return CoPlayPreIntroFadeToBlack();
        CompleteNewGame();
    }

    private void CompleteNewGame()
    {
        _newGameRoutine = null;
        CoverTitleSceneBeforeLoad();

        // 저장 유무와 무관하게 "완전 새 시작" 보장
        SaveSystem.ClearAllSaves();
        if (PlayerDataRuntime.Instance != null)
            PlayerDataRuntime.Instance.ResetToDefaults();

        // (유지) 기존 컨텍스트도 그대로
        GameStartContext.SetNewGame();

        LoadMyRoom(MyroomEntryPoint.Spawn_Room1_NewGame);
    }

    // 버튼: 이어하기
    public void LoadGame()
    {
        if (_newGameStarting)
            return;

        PlayClick();

        // (유지) 기존 컨텍스트도 그대로
        GameStartContext.SetLoadGame();
        if (PlayerDataRuntime.Instance != null)
            PlayerDataRuntime.Instance.LoadFromDisk();

        LoadMyRoom(MyroomEntryPoint.Spawn_Room2_LoadGame_PlayerDead);
    }

    private void LoadMyRoom(MyroomEntryPoint entryPoint)
    {
        SceneTransitionService.EnterMyroom(entryPoint, myRoomSceneName, useFaderIfExists: true);
    }

    private void PlayClick()
    {
        if (sfxPlayer != null && clickSound != null)
            sfxPlayer.PlayOneShot(clickSound);
    }

    private void CoverTitleSceneBeforeLoad()
    {
        if (!usePreIntroBlackFade || !ResolvePreIntroFadeUi())
            return;

        BringPreIntroFadeToFront();
        SetPreIntroFadeAlpha(1f, true);
    }

    private IEnumerator CoPlayPreIntroFadeToBlack()
    {
        if (!usePreIntroBlackFade || !ResolvePreIntroFadeUi())
            yield break;

        BringPreIntroFadeToFront();
        yield return CoFadePreIntroOverlay(0f, 1f, preIntroFadeToBlackSeconds);

        if (preIntroBlackHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(preIntroBlackHoldSeconds);
    }

    private IEnumerator CoFadePreIntroOverlayOut()
    {
        if (!usePreIntroBlackFade || preIntroFadeGroup == null)
            yield break;

        BringPreIntroFadeToFront();
        yield return CoFadePreIntroOverlay(1f, 0f, preIntroFadeFromBlackSeconds);
        SetPreIntroFadeAlpha(0f, false);
    }

    private IEnumerator CoFadePreIntroOverlay(float fromAlpha, float toAlpha, float duration)
    {
        SetPreIntroFadeAlpha(fromAlpha, true);

        if (duration <= 0f)
        {
            SetPreIntroFadeAlpha(toAlpha, toAlpha > 0.0001f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
            SetPreIntroFadeAlpha(alpha, true);
            yield return null;
        }

        SetPreIntroFadeAlpha(toAlpha, toAlpha > 0.0001f);
    }

    private IEnumerator CoPlayReturnFadeIn(float duration)
    {
        yield return CoFadeReturnOverlay(1f, 0f, duration);
        SetReturnFadeAlpha(0f, false);
        _returnFadeRoutine = null;
    }

    private IEnumerator CoFadeReturnOverlay(float fromAlpha, float toAlpha, float duration)
    {
        SetReturnFadeAlpha(fromAlpha, true);

        if (duration <= 0f)
        {
            SetReturnFadeAlpha(toAlpha, toAlpha > 0.0001f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
            SetReturnFadeAlpha(alpha, true);
            yield return null;
        }

        SetReturnFadeAlpha(toAlpha, toAlpha > 0.0001f);
    }

    private bool ResolvePreIntroFadeUi()
    {
        if (preIntroFadeGroup != null)
            return true;

        if (!autoCreatePreIntroFadeIfMissing)
            return false;

        if (preIntroFadeCanvas == null)
            preIntroFadeCanvas = CreatePreIntroFadeCanvas();

        if (preIntroFadeCanvas == null)
            return false;

        GameObject panelObject = new GameObject("PreIntroBlackFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObject.transform.SetParent(preIntroFadeCanvas.transform, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = panelObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        preIntroFadeGroup = panelObject.GetComponent<CanvasGroup>();
        SetPreIntroFadeAlpha(0f, false);
        return true;
    }

    private bool ResolveReturnFadeUi()
    {
        if (returnFadeGroup != null)
            return true;

        if (!autoCreateReturnFadeIfMissing)
            return false;

        if (returnFadeCanvas == null)
            returnFadeCanvas = CreateReturnFadeCanvas();

        if (returnFadeCanvas == null)
            return false;

        GameObject panelObject = new GameObject("MainMenuReturnBlackFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObject.transform.SetParent(returnFadeCanvas.transform, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = panelObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        returnFadeGroup = panelObject.GetComponent<CanvasGroup>();
        SetReturnFadeAlpha(0f, false);
        return true;
    }

    private Canvas CreatePreIntroFadeCanvas()
    {
        GameObject canvasObject = new GameObject("PreIntroFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = preIntroFadeSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private Canvas CreateReturnFadeCanvas()
    {
        GameObject canvasObject = new GameObject("MainMenuReturnFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = returnFadeSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void BringPreIntroFadeToFront()
    {
        if (preIntroFadeCanvas != null)
            preIntroFadeCanvas.sortingOrder = preIntroFadeSortingOrder;

        if (preIntroFadeGroup != null)
            preIntroFadeGroup.transform.SetAsLastSibling();
    }

    private void BringReturnFadeToFront()
    {
        if (returnFadeCanvas != null)
            returnFadeCanvas.sortingOrder = returnFadeSortingOrder;

        if (returnFadeGroup != null)
            returnFadeGroup.transform.SetAsLastSibling();
    }

    private void SetPreIntroFadeAlpha(float alpha, bool blockRaycasts)
    {
        if (preIntroFadeGroup == null)
            return;

        preIntroFadeGroup.alpha = Mathf.Clamp01(alpha);
        preIntroFadeGroup.interactable = false;
        preIntroFadeGroup.blocksRaycasts = blockRaycasts;
    }

    private void SetReturnFadeAlpha(float alpha, bool blockRaycasts)
    {
        if (returnFadeGroup == null)
            return;

        returnFadeGroup.alpha = Mathf.Clamp01(alpha);
        returnFadeGroup.interactable = false;
        returnFadeGroup.blocksRaycasts = blockRaycasts;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
