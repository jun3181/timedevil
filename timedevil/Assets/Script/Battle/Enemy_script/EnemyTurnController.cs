// Assets/Script/Battle/Enemy_script/EnemyTurnController.cs
using System.Collections;
using UnityEngine;

public class EnemyTurnController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyDeckRuntime enemyDeck;
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private CostController cost;
    [SerializeField] private ShowCardController showCard;
    [SerializeField] private DescriptionPanelController desc;
    [SerializeField] private EnemyHandUI enemyHandUI;
    [SerializeField] private RectTransform enemyHandRect;

    [Header("Enemy Visuals")]
    [SerializeField] private EnemyRuntime enemyRuntime;
    [SerializeField] private SpriteRenderer stateEnemyRenderer;     // Enemy(tem)
    [SerializeField] private SpriteRenderer gameplayEnemyRenderer;  // none

    //  추가: 적도 Draw 효과를 실행하기 위해 DrawController 참조
    [Header("Effect Controllers")]
    [SerializeField] private DrawController drawController;
    [SerializeField] private MoveController moveController;   //  추가: Move 실행
    [SerializeField] private AttackController attackController;
    [SerializeField] private SupportController supportController;



    [Header("Timings")]
    [SerializeField] private bool showCardPreviewEnabled = false;
    [SerializeField] private float previewSeconds = 1.2f;
    [SerializeField, Min(0f)] private float firstPlayDelaySeconds = 0.65f;
    [SerializeField, Min(0f)] private float effectSettleSeconds = 0.65f;
    [SerializeField, Min(0f)] private float playInterval = 0.15f;

    [Header("Enemy Hand Reveal")]
    [SerializeField] private bool revealHandBeforeFirstPlay = true;
    [SerializeField] private Vector2 enemyHandActiveAnchoredPosition = new Vector2(280f, -385f);
    [SerializeField] private bool convertEnemyHandPositionFromSeparatedRoot = true;
    [SerializeField] private string separatedHandRootName = "hand01";
    [SerializeField] private float handRiseYOffset = 260f;
    [SerializeField, Min(0.01f)] private float handRiseDuration = 0.22f;
    [SerializeField] private float handRiseStagger = 0.055f;
    [SerializeField] private bool handRiseFade = true;

    public static event System.Action<bool> OnEnemyAttackWindowChanged;
    private bool subscribedToEnemyRuntime;
    private bool warnedMissingVisualRefs;
    private bool warnedMissingSprites;


    void Awake()
    {
        if (!enemyDeck) enemyDeck = EnemyDeckRuntime.Instance ?? FindObjectOfType<EnemyDeckRuntime>(true);
        if (!cardDatabase) cardDatabase = Resources.Load<CardDatabaseSO>("CardDatabase");
        if (!cost) cost = FindObjectOfType<CostController>(true);
        if (!showCard) showCard = FindObjectOfType<ShowCardController>(true);
        if (!desc) desc = FindObjectOfType<DescriptionPanelController>(true);
        if (!enemyHandUI) enemyHandUI = FindObjectOfType<EnemyHandUI>(true);
        if (!enemyHandRect && enemyHandUI) enemyHandRect = enemyHandUI.GetComponent<RectTransform>();
        if (!drawController) drawController = FindObjectOfType<DrawController>(true); //  자동 결선
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);   //  자동 결선
        if (!attackController) attackController = FindObjectOfType<AttackController>(true); //  자동 결선
        if (!supportController) supportController = FindObjectOfType<SupportController>(true);

        BindEnemyVisualRenderers();
        BindEnemyRuntime();


        Debug.Log($"[EnemyTurn] Controller bound on: {gameObject.scene.name}/{gameObject.name}");
    }

    IEnumerator Start()
    {
        RefreshEnemyVisuals();

        // EnemyBootstrapper initializes the runtime in Start, so refresh once more
        // after every Start method had a chance to run.
        yield return null;
        RefreshEnemyVisuals();
    }

    void OnDisable()
    {
        UnsubscribeEnemyRuntime();
    }

    public void RefreshEnemyVisuals()
    {
        BindEnemyVisualRenderers();
        BindEnemyRuntime();
        SubscribeEnemyRuntime();

        EnemySO so = enemyRuntime ? enemyRuntime.Source : null;
        if (!so) return;

        Sprite stateSprite = so.stateSprite ? so.stateSprite : so.gameplaySprite;
        Sprite gameplaySprite = so.gameplaySprite ? so.gameplaySprite : so.stateSprite;

        if (!stateSprite && !gameplaySprite)
        {
            if (!warnedMissingSprites)
            {
                Debug.LogWarning($"[EnemyTurn] EnemySO '{so.enemyId}' has no enemy sprites. Existing scene sprites kept.");
                warnedMissingSprites = true;
            }
            return;
        }
        warnedMissingSprites = false;

        if (stateEnemyRenderer && stateSprite)
            stateEnemyRenderer.sprite = stateSprite;

        if (gameplayEnemyRenderer && gameplaySprite)
            gameplayEnemyRenderer.sprite = gameplaySprite;
    }

    private void BindEnemyRuntime()
    {
        EnemyRuntime found = enemyRuntime ? enemyRuntime : (EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true));
        if (found == enemyRuntime) return;

        UnsubscribeEnemyRuntime();
        enemyRuntime = found;
    }

    private void SubscribeEnemyRuntime()
    {
        if (subscribedToEnemyRuntime || !enemyRuntime) return;

        enemyRuntime.OnChanged -= RefreshEnemyVisuals;
        enemyRuntime.OnChanged += RefreshEnemyVisuals;
        subscribedToEnemyRuntime = true;
    }

    private void UnsubscribeEnemyRuntime()
    {
        if (subscribedToEnemyRuntime && enemyRuntime)
            enemyRuntime.OnChanged -= RefreshEnemyVisuals;

        subscribedToEnemyRuntime = false;
    }

    private void BindEnemyVisualRenderers()
    {
        if (!stateEnemyRenderer)
            stateEnemyRenderer = FindSpriteRendererUnder("Enemy(tem)");

        if (!gameplayEnemyRenderer)
            gameplayEnemyRenderer = FindSpriteRendererUnder("none");

        if ((!stateEnemyRenderer || !gameplayEnemyRenderer) && !warnedMissingVisualRefs)
        {
            Debug.LogWarning("[EnemyTurn] Enemy visual renderers are not fully bound. Assign Enemy(tem) and none renderers in EnemyTurnController.");
            warnedMissingVisualRefs = true;
        }
    }

    private static SpriteRenderer FindSpriteRendererUnder(string rootName)
    {
        foreach (Transform t in FindObjectsOfType<Transform>(true))
        {
            if (t.name != rootName || !t.gameObject.scene.IsValid()) continue;

            SpriteRenderer renderer = t.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer) return renderer;
        }

        return null;
    }

    public IEnumerator RunTurn()
    {
        if (enemyDeck == null || cost == null) yield break;

        if (enemyDeck.GetHandIds().Count < enemyDeck.MaxHandSize)
            enemyDeck.DrawOneIfNeeded();

        if (revealHandBeforeFirstPlay && enemyHandUI != null)
        {
            enemyHandUI.gameObject.SetActive(true);
            if (!enemyHandRect) enemyHandRect = enemyHandUI.GetComponent<RectTransform>();
            if (enemyHandRect) enemyHandRect.anchoredPosition = GetEnemyHandActivePosition();
            enemyHandUI.RebuildFromHand();
            yield return enemyHandUI.PlayCardsRiseStaggeredAndWait(handRiseYOffset, handRiseDuration, handRiseStagger, handRiseFade);
        }

        bool waitedBeforeFirstPlay = false;
        while (true)
        {
            var hand = enemyDeck.GetHandIds();
            if (hand == null || hand.Count == 0)
            {
                Debug.Log("[EnemyTurn] 손패가 비어 턴 종료");
                yield break;
            }

            Debug.Log($"[EnemyTurn] Hand= [{string.Join(", ", hand)}], Cost={cost.Current}");

            int playableIndex = -1;
            int playableCost = int.MaxValue;
            string playableId = null;

            for (int i = 0; i < hand.Count; i++)
            {
                string id = hand[i];
                int c = GetCardCost(id);
                Debug.Log($"[EnemyTurn] probe id={id}, cost={c}");
                if (c <= cost.Current) { playableIndex = i; playableCost = c; playableId = id; break; }
            }

            if (playableIndex < 0)
            {
                Debug.Log("[EnemyTurn] 낼 수 있는 카드가 없어 턴 종료");
                yield break;
            }

            // SO 가져오기 (타입 분기용)
            BaseCardSO so = cardDatabase ? cardDatabase.GetById(playableId) : null;

            if (so is DrawCardSO precheckDraw && drawController != null &&
                !drawController.CanExecute(precheckDraw, Faction.Enemy, selfCardsAlreadyCommitted: 1, out string drawFailMessage))
            {
                desc?.ShowOneShotMessage(drawFailMessage);
                Debug.LogWarning($"[EnemyTurn] Draw 카드 발동 실패: {drawFailMessage}");
                yield break;
            }

            if (!waitedBeforeFirstPlay)
            {
                waitedBeforeFirstPlay = true;
                yield return WaitBeforeFirstPlay();
            }

            if (!cost.TryPay(playableCost))
            {
                Debug.LogWarning("[EnemyTurn] 코스트 지불 실패 → 턴 종료");
                yield break;
            }

            Debug.Log($"[EnemyTurn] Play '{playableId}' (cost={playableCost})");

            //  설명(explanation) 고정: (explanation > display > displayName > id)
            if (desc && so)
            {
                string line =
                    !string.IsNullOrEmpty(so.explanation) ? so.explanation :
                    (!string.IsNullOrEmpty(so.display) ? so.display :
                    (!string.IsNullOrEmpty(so.displayName) ? so.displayName : so.id));
                desc.ShowTemporaryExplanation(line);
            }

            bool usedCardMovedToBottom = false;

            //  효과 실행: Draw 카드면 적 진영으로 실행 (cap 무시)
            if (so is DrawCardSO dso && drawController != null)
            {
                // (권장 UX) 먼저 프리뷰를 보여주고 …
                if (CanShowCardPreview()) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;

                // 플레이어 카드 사용 흐름과 맞춰, 효과 처리 전에 사용 카드를 손패에서 제거합니다.
                enemyDeck.UseCardToBottom(playableIndex);
                usedCardMovedToBottom = true;
                yield return null;

                // … 그 다음 Draw 효과를 '완료될 때까지' 실행
                yield return drawController.Execute(dso, Faction.Enemy);
            }
            else if (so is MoveCardSO mso && moveController != null)   //  추가된 분기
            {
                if (CanShowCardPreview()) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;

                //  적이 자신을 움직임: self=Enemy, foe=Player
                yield return moveController.Execute(mso, Faction.Enemy, Faction.Player);
            }
            else if (so is AttackCardSO aso && attackController != null)   //  추가된 부분
            {
                if (CanShowCardPreview()) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;

                //  핵심: 적이 공격 → self=Enemy, foe=Player
                OnEnemyAttackWindowChanged?.Invoke(true);
                yield return attackController.Execute(aso, Faction.Enemy, Faction.Player, skipWarningTimeline: true);
                OnEnemyAttackWindowChanged?.Invoke(false);
            }
            else if (so is SupportCardSO sso && supportController != null)
            {
                if (CanShowCardPreview()) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;

                yield return supportController.Execute(sso, Faction.Enemy, Faction.Player);
            }
            else
            {
                // Draw가 아닌 카드면 기존 프리뷰 로직
                if (CanShowCardPreview()) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;
            }

            yield return WaitForEffectSettle();

            //  설명 해제
            if (desc) desc.ClearTemporaryMessage();

            //  사용한 카드는 덱 맨 아래로
            if (!usedCardMovedToBottom)
                enemyDeck.UseCardToBottom(playableIndex);

            // (선택) 적 손패 UI 새로고침이 필요하면 여기서 호출
            // var ui = FindObjectOfType<EnemyHandUI>(true);
            // if (ui) ui.RebuildFromHand();

            if (playInterval > 0f)
                yield return new WaitForSeconds(playInterval);
        }
    }

    private int GetCardCost(string id)
    {
        if (string.IsNullOrEmpty(id) || cardDatabase == null)
        {
            Debug.LogWarning("[EnemyTurn] cost fail: empty id or CardDatabase missing");
            return 9999;
        }

        var so = cardDatabase.GetById(id);
        if (!so)
        {
            Debug.LogWarning($"[EnemyTurn] cost fail: DB miss for id='{id}'");
            return 9999;
        }

        if (so is AttackCardSO a) return Mathf.Max(0, a.cost);
        if (so is MoveCardSO m) return Mathf.Max(0, m.cost);
        if (so is SupportCardSO s) return Mathf.Max(0, s.cost);
        if (so is BaseCardSO b) return Mathf.Max(0, b.cost);

        var t = so.GetType();
        const System.Reflection.BindingFlags BF =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        var f = t.GetField("cost", BF) ?? t.GetField("Cost", BF);
        if (f != null && f.FieldType == typeof(int))
            return Mathf.Max(0, (int)f.GetValue(so));

        var p = t.GetProperty("cost", BF) ?? t.GetProperty("Cost", BF);
        if (p != null && p.PropertyType == typeof(int) && p.CanRead)
            return Mathf.Max(0, (int)p.GetValue(so));

        Debug.LogWarning($"[EnemyTurn] cost fail: type '{t.Name}' has no int cost for id='{id}'");
        return 9999;
    }

    private Vector2 GetEnemyHandActivePosition()
    {
        return HandPositionUtility.ToSeparatedRootLocal(
            enemyHandRect,
            enemyHandActiveAnchoredPosition,
            convertEnemyHandPositionFromSeparatedRoot,
            separatedHandRootName);
    }

    private bool CanShowCardPreview()
    {
        return showCardPreviewEnabled && showCard != null;
    }

    private IEnumerator WaitBeforeFirstPlay()
    {
        if (firstPlayDelaySeconds > 0f)
            yield return new WaitForSeconds(firstPlayDelaySeconds);
    }

    private IEnumerator WaitForEffectSettle()
    {
        if (effectSettleSeconds > 0f)
            yield return new WaitForSeconds(effectSettleSeconds);
    }
}
