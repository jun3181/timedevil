// Assets/Script/Battle/Card_script/CardAnimeController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class CardAnimeController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HandUI playerHandUI;
    [SerializeField] private EnemyHandUI enemyHandUI;

    [Header("Motion")]
    [Tooltip("카드가 내려올 시작 오프셋(+Y에서 0으로)")]
    [SerializeField] private float startYOffset = 361f;
    [Tooltip("각 카드 1장의 이동/페이드 시간")]
    [SerializeField] private float perCardDuration = 0.25f;
    [Tooltip("카드 사이 시작 간격(스태거)")]
    [SerializeField] private float perCardStagger = 0.15f;

    [Header("Visual")]
    [Tooltip("애니메이션 중 알파 0→1로 페이드")]
    [SerializeField] private bool fadeAlpha = true;

    bool busy;

    void Reset()
    {
        if (!playerHandUI) playerHandUI = FindObjectOfType<HandUI>(true);
        if (!enemyHandUI) enemyHandUI = FindObjectOfType<EnemyHandUI>(true);
    }

    // ====== 기존 드로우(내려오기) 연출 ======
    public Coroutine RevealInitialPlayerHand() => StartCoroutine(Co_RevealHand(playerHandUI));
    public Coroutine RevealInitialEnemyHand() => StartCoroutine(Co_RevealHand(enemyHandUI));

    private IEnumerator Co_RevealHand(object handUI)
    {
        if (busy || handUI == null) yield break;
        busy = true;

        List<RectTransform> rects = null;
        if (handUI is HandUI p)
        {
            p.ShowCards();
            rects = p.GetAllCardRects();
        }
        else if (handUI is EnemyHandUI e)
        {
            e.ShowAll();
            rects = e.GetAllCardRects();
        }

        if (rects == null || rects.Count == 0) { busy = false; yield break; }

        var baselines = new List<Vector2>(rects.Count);
        for (int i = 0; i < rects.Count; i++)
        {
            var rt = rects[i];
            baselines.Add(rt.anchoredPosition);
            rt.anchoredPosition = baselines[i] + new Vector2(0f, -startYOffset);

            if (fadeAlpha)
            {
                var cg = rt.GetComponent<CanvasGroup>();
                if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        var running = new List<Coroutine>(rects.Count);
        for (int i = 0; i < rects.Count; i++)
        {
            running.Add(StartCoroutine(Co_TweenCard(rt: rects[i], targetPos: baselines[i])));
            if (perCardStagger > 0f) yield return new WaitForSeconds(perCardStagger);
        }
        foreach (var c in running) if (c != null) yield return c;

        busy = false;
    }

    private IEnumerator Co_TweenCard(RectTransform rt, Vector2 targetPos)
    {
        float t = 0f;
        var startPos = rt.anchoredPosition;
        CanvasGroup cg = fadeAlpha ? rt.GetComponent<CanvasGroup>() : null;

        while (t < perCardDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / perCardDuration);
            float e = 1f - Mathf.Pow(1f - u, 3f);

            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, e);
            if (cg) cg.alpha = Mathf.LerpUnclamped(0f, 1f, e);
            yield return null;
        }
        rt.anchoredPosition = targetPos;
        if (cg) cg.alpha = 1f;
    }

    /// <summary>드로우 등으로 방금 추가된 카드들만 애니메이션</summary>
    public Coroutine RevealLastNCards(Faction side, int n)
    {
        if (n <= 0) return null;

        if (side == Faction.Player)
            return StartCoroutine(Co_RevealLastN_FromHandUI(playerHandUI, n));
        else
            return StartCoroutine(Co_RevealLastN_FromEnemyUI(enemyHandUI, n));
    }

    private IEnumerator Co_RevealLastN_FromHandUI(HandUI ui, int n)
    {
        if (ui == null) yield break;
        yield return new WaitForEndOfFrame();

        var rectsAll = ui.GetAllCardRects();
        if (rectsAll == null || rectsAll.Count == 0) yield break;

        int start = Mathf.Max(0, rectsAll.Count - n);
        var subset = rectsAll.GetRange(start, rectsAll.Count - start);

        yield return StartCoroutine(Co_RevealSubset(subset));
    }

    private IEnumerator Co_RevealLastN_FromEnemyUI(EnemyHandUI ui, int n)
    {
        if (ui == null) yield break;
        yield return new WaitForEndOfFrame();

        var rectsAll = ui.GetAllCardRects();
        if (rectsAll == null || rectsAll.Count == 0) yield break;

        int start = Mathf.Max(0, rectsAll.Count - n);
        var subset = rectsAll.GetRange(start, rectsAll.Count - start);

        yield return StartCoroutine(Co_RevealSubset(subset));
    }

    private IEnumerator Co_RevealSubset(List<RectTransform> subset)
    {
        if (subset == null || subset.Count == 0) yield break;

        for (int i = 0; i < subset.Count; i++)
        {
            var rt = subset[i];
            rt.anchoredPosition += new Vector2(0f, -startYOffset);

            if (fadeAlpha)
            {
                var cg = rt.GetComponent<CanvasGroup>();
                if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        var coros = new List<Coroutine>(subset.Count);
        for (int i = 0; i < subset.Count; i++)
        {
            var target = subset[i].anchoredPosition + new Vector2(0f, startYOffset);
            coros.Add(StartCoroutine(Co_TweenCard(subset[i], target)));
            if (perCardStagger > 0f) yield return new WaitForSeconds(perCardStagger);
        }
        foreach (var c in coros) if (c != null) yield return c;
    }

    // ====== 새로 추가: 버림(역연출: 위로 + 페이드아웃) ======

    /// <summary>
    /// 마지막 N장을 역연출로 숨긴 뒤(afterAnim) 콜백 실행(데이터 변경), 이후 리빌드까지 수행.
    /// fromRight=true면 오른쪽(마지막)부터 N장.
    /// </summary>
    public Coroutine DiscardLastNCards(Faction side, int n, bool fromRight, Action afterAnimDataOp = null)
    {
        if (n <= 0) return null;

        if (side == Faction.Player)
            return StartCoroutine(Co_DiscardLastN_FromUI_Player(playerHandUI, n, fromRight, afterAnimDataOp));
        else
            return StartCoroutine(Co_DiscardLastN_FromUI_Enemy(enemyHandUI, n, fromRight, afterAnimDataOp));
    }

    /// <summary>플레이어 손패의 특정 인덱스 한 장을 역연출로 숨기고 콜백→리빌드.</summary>
    public Coroutine DiscardOneAtIndex(Faction side, int index, Action afterAnimDataOp = null)
    {
        if (side == Faction.Player)
            return StartCoroutine(Co_DiscardOne_ByIndex(playerHandUI, index, afterAnimDataOp));
        else
            return StartCoroutine(Co_DiscardOne_ByIndex(enemyHandUI, index, afterAnimDataOp));
    }

    // ---- 내부: 역연출 공통부 ----
    private IEnumerator Co_DiscardOne_ByIndex(object ui, int index, Action afterAnimDataOp)
    {
        yield return new WaitForEndOfFrame();

        List<RectTransform> rects = null;
        if (ui is HandUI p)
        {
            rects = p.GetAllCardRects();
        }
        else if (ui is EnemyHandUI e)
        {
            rects = e.GetAllCardRects();
        }
        if (rects == null || rects.Count == 0) yield break;
        index = Mathf.Clamp(index, 0, rects.Count - 1);

        yield return StartCoroutine(Co_DiscardSubset(new List<RectTransform> { rects[index] }));

        afterAnimDataOp?.Invoke();            // 실제 데이터 이동(덱 아래 등)
        yield return null;                    // 데이터 반영 프레임
        if (ui is HandUI p2) p2.RebuildFromHand();
        else if (ui is EnemyHandUI e2) e2.RebuildFromHand();
    }

    private IEnumerator Co_DiscardLastN_FromUI_Player(HandUI ui, int n, bool fromRight, Action afterAnimDataOp)
    {
        if (ui == null) yield break;
        yield return new WaitForEndOfFrame();

        var rects = ui.GetAllCardRects();
        if (rects == null || rects.Count == 0) yield break;

        var subset = GetTailSubset(rects, n, fromRight);
        yield return StartCoroutine(Co_DiscardSubset(subset));

        afterAnimDataOp?.Invoke();
        yield return null;
        ui.RebuildFromHand();
    }

    private IEnumerator Co_DiscardLastN_FromUI_Enemy(EnemyHandUI ui, int n, bool fromRight, Action afterAnimDataOp)
    {
        if (ui == null) yield break;
        yield return new WaitForEndOfFrame();

        var rects = ui.GetAllCardRects();
        if (rects == null || rects.Count == 0) yield break;

        var subset = GetTailSubset(rects, n, fromRight);
        yield return StartCoroutine(Co_DiscardSubset(subset));

        afterAnimDataOp?.Invoke();
        yield return null;
        ui.RebuildFromHand();
    }

    private List<RectTransform> GetTailSubset(List<RectTransform> rects, int n, bool fromRight)
    {
        n = Mathf.Clamp(n, 0, rects.Count);
        var subset = new List<RectTransform>(n);
        if (fromRight)
        {
            for (int i = rects.Count - n; i < rects.Count; i++) subset.Add(rects[i]);
        }
        else
        {
            for (int i = 0; i < n; i++) subset.Add(rects[i]);
        }
        return subset;
    }

    // 위로 이동 + 페이드아웃
    private IEnumerator Co_DiscardSubset(List<RectTransform> subset)
    {
        if (subset == null || subset.Count == 0) yield break;

        // 초기 상태: 알파 1 유지(기존 상태), 목표는 위로 이동하며 알파 0
        var coros = new List<Coroutine>(subset.Count);
        for (int i = 0; i < subset.Count; i++)
        {
            var rt = subset[i];
            var cg = fadeAlpha ? (rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>()) : null;
            if (cg) cg.alpha = 1f;

            var start = rt.anchoredPosition;
            var target = start + new Vector2(0f, startYOffset);
            coros.Add(StartCoroutine(Co_TweenCardReverse(rt, start, target, cg)));
            if (perCardStagger > 0f) yield return new WaitForSeconds(perCardStagger);
        }

        foreach (var c in coros) if (c != null) yield return c;
    }

    private IEnumerator Co_TweenCardReverse(RectTransform rt, Vector2 startPos, Vector2 targetPos, CanvasGroup cg)
    {
        float t = 0f;
        while (t < perCardDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / perCardDuration);
            float e = 1f - Mathf.Pow(1f - u, 3f); // 동일 ease

            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, e);
            if (cg) cg.alpha = Mathf.LerpUnclamped(1f, 0f, e);
            yield return null;
        }
        rt.anchoredPosition = targetPos;
        if (cg) cg.alpha = 0f;
    }
}
