using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// 카드/덱 화면에서 슬롯 그려주고 선택/이동 처리.
/// - Card 패널: 보유(owned) 중 덱에 없는 카드들
/// - Deck 패널: 덱 목록 그대로
/// - E키: 현재 영역에서 반대 영역으로 한 장 이동
/// - 덱은 중복 불가 + 최대 13장 제한
/// - W키: 이전(메인) 씬으로 복귀 (PlayerReturnContext 사용)
/// </summary>
public class CardSceneController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private Transform cardPanel;    // 보유 카드 영역
    [SerializeField] private Transform deckPanel;    // 덱 영역
    [SerializeField] private Image explainImage;     // 확대 미리보기
    [SerializeField] private RectTransform selector; // 주황 박스

    [Header("Prefab & Resources")]
    [SerializeField] private GameObject cardSlotPrefab;              // Image 하나 들어있는 프리팹
    [SerializeField] private string resourcesFolder = "my_asset";    // Resources/my_asset/<CardId>

    [Header("Return Settings")]
    [SerializeField] private float graceSeconds = 1.0f;              // 복귀 직후 충돌 무적 시간(옵션)
    [SerializeField] private string worldVcamName = "CM vcam1";      // 복귀 씬에서 쓸 vcam 이름(없으면 자동 탐색)
    [SerializeField] private bool useFaderIfExists = true;           // SceneFader 있으면 사용

    // 내부 상태
    private readonly List<CardSlot> cardSlots = new();
    private readonly List<CardSlot> deckSlots = new();
    private int currentIndex = 0;
    private bool inDeck = false; // false = Card영역, true = Deck영역

    void Start()
    {
        var runtime = CardStateRuntime.Instance;
        var data = runtime != null ? runtime.Data : new CardSaveData();

        var owned = data.owned ?? new List<string>();
        var deck = data.deck ?? new List<string>();

        // Card 패널: owned - deck
        foreach (var id in owned.Where(id => !deck.Contains(id)))
            AddSlotToPanel(cardPanel, cardSlots, id);

        // Deck 패널: deck 그대로
        foreach (var id in deck)
            AddSlotToPanel(deckPanel, deckSlots, id);

        UpdateSelector();
        UpdateExplain();
    }

    void Update()
    {
        HandleInput();
    }

    // ----------------------------------------------------

    void HandleInput()
    {
        // W: 이전 씬(메인)으로 복귀 — PlayerReturnContext 기반
        if (Input.GetKeyDown(KeyCode.W))
        {
            // 복귀 대상이 세팅되어 있으면 표준 복귀 루트 사용
            if (!string.IsNullOrEmpty(PlayerReturnContext.ReturnSceneName))
            {
                // 카메라 재바인딩 요청
                PlayerReturnContext.CameraRebindRequested = true;
                PlayerReturnContext.TargetVcamName = string.IsNullOrWhiteSpace(worldVcamName) ? null : worldVcamName;

                // (옵션) 복귀 직후 무적
                if (graceSeconds > 0f)
                {
                    PlayerReturnContext.IsInGracePeriod = true;
                    PlayerReturnContext.GraceSecondsPending = graceSeconds;
                }
                else
                {
                    PlayerReturnContext.IsInGracePeriod = false;
                    PlayerReturnContext.GraceSecondsPending = 0f;
                }

                // 페이더 우선 복귀
                SceneLoader.GoBackToReturnScene(graceSeconds, useFaderIfExists);
            }
            else
            {
                // 폴백: 마지막 씬 기록이 있으면 그리로, 아니면 경고
                if (!string.IsNullOrEmpty(SceneHistory.LastSceneName))
                {
                    if (SceneFader.instance) SceneFader.instance.LoadSceneWithFade(SceneHistory.LastSceneName);
                    else SceneManager.LoadScene(SceneHistory.LastSceneName);
                }
                else
                {
                    Debug.LogWarning("[CardScene] 복귀 대상(ReturnSceneName)이 설정되지 않았습니다.");
                }
            }
            return;
        }

        var list = inDeck ? deckSlots : cardSlots;

        if (list.Count == 0)
        {
            UpdateSelector();
            UpdateExplain();
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                inDeck = !inDeck;
                currentIndex = 0;
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentIndex = (currentIndex + 1) % list.Count;
            UpdateSelector(); UpdateExplain();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentIndex = (currentIndex - 1 + list.Count) % list.Count;
            UpdateSelector(); UpdateExplain();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            inDeck = !inDeck;
            currentIndex = 0;
            UpdateSelector(); UpdateExplain();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (!inDeck) MoveCard_toDeck_and_RemoveFromCard();
            else MoveCard_toCard_and_RemoveFromDeck();
        }
    }

    // ----- 이동 로직 -----

    // Card → Deck (중복 불가 + 13장 제한)
    void MoveCard_toDeck_and_RemoveFromCard()
    {
        if (cardSlots.Count == 0) return;

        var slot = cardSlots[currentIndex];
        var id = slot.cardId;
        if (string.IsNullOrEmpty(id)) return;

        var rt = CardStateRuntime.Instance;
        if (rt == null) { Debug.LogWarning("[CardScene] CardStateRuntime 없음"); return; }

        if (!rt.TryAddToDeck(id))
        {
            if (rt.DeckContains(id))
                Debug.LogWarning("[CardScene] 이미 덱에 있는 카드입니다.");
            else if (rt.DeckCount >= CardStateRuntime.MAX_DECK)
                Debug.LogWarning($"[CardScene] 덱이 가득 찼습니다. (최대 {CardStateRuntime.MAX_DECK}장)");
            return;
        }

        // 덱 UI에 추가
        AddSlotToPanel(deckPanel, deckSlots, id);

        // 카드 패널에서 제거
        var removedGO = slot.gameObject;
        cardSlots.RemoveAt(currentIndex);
        Destroy(removedGO);

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, cardSlots.Count - 1));
        UpdateSelector();
        UpdateExplain();
    }

    // Deck → Card (되돌리기)
    void MoveCard_toCard_and_RemoveFromDeck()
    {
        if (deckSlots.Count == 0) return;

        var slot = deckSlots[currentIndex];
        var id = slot.cardId;
        if (string.IsNullOrEmpty(id)) return;

        var rt = CardStateRuntime.Instance;
        if (rt != null) rt.RemoveFromDeck(id);

        AddSlotToPanel(cardPanel, cardSlots, id);

        var removedGO = slot.gameObject;
        deckSlots.RemoveAt(currentIndex);
        Destroy(removedGO);

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, deckSlots.Count - 1));
        UpdateSelector();
        UpdateExplain();
    }

    // ----- 슬롯/UI -----

    void AddSlotToPanel(Transform parent, List<CardSlot> list, string cardId)
    {
        var go = Instantiate(cardSlotPrefab, parent);
        var slot = go.GetComponent<CardSlot>();
        if (!slot) slot = go.AddComponent<CardSlot>();

        var sprite = Resources.Load<Sprite>($"{resourcesFolder}/{cardId}");
        slot.Setup(cardId, sprite);

        list.Add(slot);
    }

    void UpdateSelector()
    {
        var list = inDeck ? deckSlots : cardSlots;
        if (list.Count == 0) return;

        currentIndex = Mathf.Clamp(currentIndex, 0, list.Count - 1);
        if (selector) selector.position = list[currentIndex].transform.position;
    }

    void UpdateExplain()
    {
        var list = inDeck ? deckSlots : cardSlots;
        if (explainImage == null) return;

        if (list.Count == 0) { explainImage.sprite = null; return; }

        var slot = list[currentIndex];
        explainImage.sprite = slot.image ? slot.image.sprite : null;
    }
}
