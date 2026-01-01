// DrawController.cs
using System.Collections;
using UnityEngine;

public class DrawController : MonoBehaviour
{
    [Header("Optional VFX (UpDraw only, once)")]
    [SerializeField] private GameObject upDrawParticlePrefab;
    [SerializeField] private float vfxLifetime = 1.2f;

    [Header("Anchors (where to spawn VFX)")]
    [SerializeField] private Transform playerHandAnchor;
    [SerializeField] private Transform enemyHandAnchor;

    [Header("Anime & UI Refs")]
    [SerializeField] private CardAnimeController cardAnime;
    [SerializeField] private BattleMenuController menu;                 // 👈 추가
    [SerializeField] private DescriptionPanelController desc;           // 👈 추가 (관전모드 토글용)
    [SerializeField] private HandUI playerHandUI;                       // 👈 추가
    [SerializeField] private EnemyHandUI enemyHandUI;                   // 👈 추가

    public IEnumerator Execute(DrawCardSO so, Faction self)
    {
        if (so == null) yield break;

        // ----------------------------------------------------
        // 1) UpDraw: cap 무시 드로우 → 아래에서 위로 등장 애니
        // ----------------------------------------------------
        if (so.drawMode == DrawMode.UpDraw)
        {
            int actuallyDrawn = 0;

            if (self == Faction.Player)
            {
                var deck = BattleDeckRuntime.Instance;
                if (deck != null) actuallyDrawn = deck.Draw(so.amount, ignoreHandCap: true);
                else Debug.LogWarning("[DrawController] BattleDeckRuntime is null (player).");
            }
            else
            {
                var enemy = EnemyDeckRuntime.Instance;
                if (enemy != null) actuallyDrawn = enemy.Draw(so.amount, ignoreHandCap: true);
                else Debug.LogWarning("[DrawController] EnemyDeckRuntime is null (enemy).");
            }

            // VFX
            if (upDrawParticlePrefab != null)
            {
                Transform anchor = (self == Faction.Player) ? playerHandAnchor : enemyHandAnchor;
                Vector3 pos = anchor ? anchor.position : Vector3.zero;
                var go = Instantiate(upDrawParticlePrefab, pos, Quaternion.identity);
                if (vfxLifetime > 0f) Destroy(go, vfxLifetime);
            }

            if (actuallyDrawn > 0 && cardAnime != null)
            {
                yield return new WaitForEndOfFrame();
                cardAnime.RevealLastNCards(self, actuallyDrawn);
            }
            yield break;
        }

        // ----------------------------------------------------
        // 2) AntiDraw:
        //    - 카드 선택모드에서 발동
        //    - 상대 손패를 보여주고(관전모드), 내 손패는 비활성
        //    - 랜덤 카드 n장을 위로 페이드아웃(애니) → 덱 밑으로 이동
        //    - 끝나면 상대 손패 숨김, 내 손패 활성, 카드 선택모드 재진입
        // ----------------------------------------------------
        if (so.drawMode == DrawMode.AntiDraw)
        {
            var targetSide = (self == Faction.Player) ? Faction.Enemy : Faction.Player;

            // 헬퍼: 현재 대상 손패 장수
            int TargetHandCount()
            {
                {
                    if (targetSide == Faction.Enemy) return EnemyDeckRuntime.Instance ? EnemyDeckRuntime.Instance.GetHandIds().Count : 0;
                    else return BattleDeckRuntime.Instance ? BattleDeckRuntime.Instance.HandCount : 0;
                }
            }

            // 헬퍼: 대상 손패 idx를 덱 밑으로
            System.Action<int> DiscardToBottomAt = (idx) =>
            {
                if (targetSide == Faction.Enemy) { var rt = EnemyDeckRuntime.Instance; if (rt != null) rt.DiscardToBottom(idx); }
                else { var rt = BattleDeckRuntime.Instance; if (rt != null) rt.DiscardToBottom(idx); }
            };

            // ===== 관전모드 진입 =====
            // 메뉴 입력 잠그고, 내 손패는 숨김/상호작용 비활성, 상대 손패는 표시
            if (menu) menu.EnableInput(false);

            // DescriptionPanel의 강제 EnemyTurn 모드를 임시로 사용하여
            // - PlayerHand는 OFF
            // - EnemyHand는 ON
            // 을 한 큐에 처리 (해당 컨트롤러가 적절히 CanvasGroup을 처리함)
            if (desc) desc.EnterSpectate(
                showSide: targetSide,
                message: "상대 손패에서 무작위 카드를 제거합니다..."
            );
            else
            {
                // 폴백 (desc 없을 때)
                if (targetSide == Faction.Player)
                {
                    if (playerHandUI) playerHandUI.ShowCards();
                    if (enemyHandUI) enemyHandUI.HideAll();
                }
                else
                {
                    if (enemyHandUI) { enemyHandUI.gameObject.SetActive(true); enemyHandUI.ShowAll(); }
                    if (playerHandUI) playerHandUI.HideCards();
                }
            }

            // 한 프레임 대기(손패 UI가 켜지고 RectTransform이 준비되도록)
            yield return new WaitForEndOfFrame();

            // 실제 제거 수 계산
            int available = TargetHandCount();
            int removeN = Mathf.Clamp(so.amount, 0, available);

            // 애니메이션 + 데이터 이동(직렬 처리: 한 장씩 보여주기)
            for (int t = 0; t < removeN; t++)
            {
                int countNow = TargetHandCount();
                if (countNow <= 0) break;

                int idx = Random.Range(0, countNow);

                if (cardAnime != null)
                {
                    // 위로 이동 + 페이드아웃 → 끝나면 실제로 덱 밑으로 이동
                    yield return cardAnime.DiscardOneAtIndex(
                        targetSide,
                        idx,
                        afterAnimDataOp: () => DiscardToBottomAt(idx)
                    );
                }
                else
                {
                    // 애니 컨트롤러 없으면 즉시 이동만
                    DiscardToBottomAt(idx);
                    yield return null;
                }
            }

            // 살짝 여유 프레임 (리빌드 반영)
            yield return null;

            // ===== 관전모드 해제 & 원상복구 =====
            if (desc) desc.ExitSpectate();
            else
            {
                // 폴백 원복
                if (targetSide == Faction.Player)
                {
                    // 방금 Player 손패를 보여줬으니 끄고, 적 손패는 적턴이라면 다시 켜질 것
                    if (playerHandUI) playerHandUI.HideCards();
                    if (enemyHandUI) { enemyHandUI.gameObject.SetActive(true); enemyHandUI.ShowAll(); }
                }
                else
                {
                    if (enemyHandUI) enemyHandUI.HideAll();
                    if (playerHandUI) playerHandUI.ShowCards();
                }
            }

            // 🔁 적이 사용한 경우: 적 턴 화면으로 즉시 복귀 보장
            if (self == Faction.Enemy && desc) desc.SetEnemyTurn(true);

            // 플레이어가 사용한 경우만 카드 선택모드로 복귀
            if (self == Faction.Player && playerHandUI)
            {
                if (menu) menu.EnableInput(false);
                playerHandUI.EnterSelectMode();
            }

            yield break;
        }

        Debug.LogWarning($"[DrawController] Unknown drawMode: {so.drawMode}");
    }

    public void SetAnchors(Transform player, Transform enemy)
    {
        playerHandAnchor = player;
        enemyHandAnchor = enemy;
    }
}
