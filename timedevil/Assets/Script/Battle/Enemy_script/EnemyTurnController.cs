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

    // 👇 추가: 적도 Draw 효과를 실행하기 위해 DrawController 참조
    [Header("Effect Controllers")]
    [SerializeField] private DrawController drawController;
    [SerializeField] private MoveController moveController;   // ⭐ 추가: Move 실행
    [SerializeField] private AttackController attackController;



    [Header("Timings")]
    [SerializeField] private float previewSeconds = 1.2f;
    [SerializeField] private float playInterval = 0.15f;

    public static event System.Action<bool> OnEnemyAttackWindowChanged;


    void Awake()
    {
        if (!enemyDeck) enemyDeck = EnemyDeckRuntime.Instance ?? FindObjectOfType<EnemyDeckRuntime>(true);
        if (!cardDatabase) cardDatabase = Resources.Load<CardDatabaseSO>("CardDatabase");
        if (!cost) cost = FindObjectOfType<CostController>(true);
        if (!showCard) showCard = FindObjectOfType<ShowCardController>(true);
        if (!desc) desc = FindObjectOfType<DescriptionPanelController>(true);
        if (!drawController) drawController = FindObjectOfType<DrawController>(true); // ⭐ 자동 결선
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);   // ⭐ 자동 결선
        if (!attackController) attackController = FindObjectOfType<AttackController>(true); // ✅ 자동 결선



        Debug.Log($"[EnemyTurn] Controller bound on: {gameObject.scene.name}/{gameObject.name}");
    }

    public IEnumerator RunTurn()
    {
        if (enemyDeck == null || cost == null) yield break;

        if (enemyDeck.GetHandIds().Count < enemyDeck.MaxHandSize)
            enemyDeck.DrawOneIfNeeded();

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

            if (!cost.TryPay(playableCost))
            {
                Debug.LogWarning("[EnemyTurn] 코스트 지불 실패 → 턴 종료");
                yield break;
            }

            Debug.Log($"[EnemyTurn] Play '{playableId}' (cost={playableCost})");

            // SO 가져오기 (타입 분기용)
            BaseCardSO so = cardDatabase ? cardDatabase.GetById(playableId) : null;

            // ▶ 설명(explanation) 고정: (explanation > display > displayName > id)
            if (desc && so)
            {
                string line =
                    !string.IsNullOrEmpty(so.explanation) ? so.explanation :
                    (!string.IsNullOrEmpty(so.display) ? so.display :
                    (!string.IsNullOrEmpty(so.displayName) ? so.displayName : so.id));
                desc.ShowTemporaryExplanation(line);
            }

            // ▶ 효과 실행: Draw 카드면 적 진영으로 실행 (cap 무시)
            if (so is DrawCardSO dso && drawController != null)
            {
                // (권장 UX) 먼저 프리뷰를 보여주고 …
                if (showCard != null) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;

                // … 그 다음 Draw 효과를 '완료될 때까지' 실행
                yield return drawController.Execute(dso, Faction.Enemy);
            }
            else if (so is MoveCardSO mso && moveController != null)   // ⭐⭐ 추가된 분기
            {
                if (showCard != null) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;

                // 🔶 적이 자신을 움직임: self=Enemy, foe=Player
                yield return moveController.Execute(mso, Faction.Enemy, Faction.Player);
            }
            else if (so is AttackCardSO aso && attackController != null)   // ✅ 추가된 부분
            {
                if (showCard != null) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;

                // 🔥 핵심: 적이 공격 → self=Enemy, foe=Player
                OnEnemyAttackWindowChanged?.Invoke(true);
                yield return attackController.Execute(aso, Faction.Enemy, Faction.Player);
                OnEnemyAttackWindowChanged?.Invoke(false);
            }
            else
                    {
                // Draw가 아닌 카드면 기존 프리뷰 로직
                if (showCard != null) yield return showCard.PreviewById(playableId, previewSeconds);
                else yield return null;
            }

            // ▶ 설명 해제
            if (desc) desc.ClearTemporaryMessage();

            // ▶ 사용한 카드는 덱 맨 아래로
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
}
