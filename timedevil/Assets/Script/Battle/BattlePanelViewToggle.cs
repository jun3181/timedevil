using System.Collections;
using UnityEngine;

/// <summary>
/// 패널의 특정 메뉴 항목(E 제출)으로 전투 시점을 토글한다.
/// - 기본 상태: 적(1인칭 연출용)이 보이고 게임 화면은 아래쪽
/// - 토글 상태: 적은 아래로 내려가 화면 밖, 게임 화면은 위로 올라옴
/// </summary>
public class BattlePanelViewToggle : MonoBehaviour
{
    public enum EaseType
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic
    }

    [Header("Trigger")]
    [SerializeField] private BattleMenuController menu;
    [Tooltip("onSubmit(index)에서 이 인덱스일 때 토글")]
    [SerializeField] private int triggerMenuIndex = 2;

    [Header("Targets")]
    [SerializeField] private Transform enemyTarget;
    [SerializeField] private Transform gameplayTarget;
    [SerializeField] private bool useLocalPosition = true;

    [Header("Offsets")]
    [Tooltip("토글 ON(게임 화면 활성)일 때 적 오브젝트에 더해질 오프셋")]
    [SerializeField] private Vector3 enemyHiddenOffset = new Vector3(0f, -600f, 0f);
    [Tooltip("토글 ON(게임 화면 활성)일 때 게임 화면에 더해질 오프셋")]
    [SerializeField] private Vector3 gameplayShownOffset = new Vector3(0f, 260f, 0f);

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float duration = 0.35f;
    [SerializeField] private EaseType ease = EaseType.EaseInOutCubic;

    [Header("State")]
    [Tooltip("체크 시 시작부터 게임 화면이 올라온 상태")]
    [SerializeField] private bool startInGameplayView = false;

    private Vector3 enemyBasePos;
    private Vector3 gameplayBasePos;

    private bool isGameplayView;
    private bool isAnimating;
    private Coroutine running;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
    }

    void Awake()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);

        enemyBasePos = GetPos(enemyTarget);
        gameplayBasePos = GetPos(gameplayTarget);

        isGameplayView = startInGameplayView;
        ApplyImmediate(isGameplayView);
    }

    void OnEnable()
    {
        if (menu) menu.onSubmit.AddListener(OnMenuSubmit);
    }

    void OnDisable()
    {
        if (menu) menu.onSubmit.RemoveListener(OnMenuSubmit);
    }

    private void OnMenuSubmit(int index)
    {
        if (index != triggerMenuIndex) return;
        ToggleView();
    }

    public void ToggleView()
    {
        if (isAnimating) return;

        isGameplayView = !isGameplayView;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Co_Animate(isGameplayView));
    }

    public void SetGameplayView(bool on, bool immediate = false)
    {
        if (isAnimating && running != null)
        {
            StopCoroutine(running);
            running = null;
            isAnimating = false;
        }

        isGameplayView = on;
        if (immediate) ApplyImmediate(on);
        else running = StartCoroutine(Co_Animate(on));
    }

    private IEnumerator Co_Animate(bool gameplayView)
    {
        isAnimating = true;

        Vector3 enemyFrom = GetPos(enemyTarget);
        Vector3 gameFrom = GetPos(gameplayTarget);

        Vector3 enemyTo = gameplayView ? enemyBasePos + enemyHiddenOffset : enemyBasePos;
        Vector3 gameTo = gameplayView ? gameplayBasePos + gameplayShownOffset : gameplayBasePos;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = EvaluateEase(ease, k);

            if (enemyTarget) SetPos(enemyTarget, Vector3.LerpUnclamped(enemyFrom, enemyTo, e));
            if (gameplayTarget) SetPos(gameplayTarget, Vector3.LerpUnclamped(gameFrom, gameTo, e));

            yield return null;
        }

        if (enemyTarget) SetPos(enemyTarget, enemyTo);
        if (gameplayTarget) SetPos(gameplayTarget, gameTo);

        running = null;
        isAnimating = false;
    }

    private void ApplyImmediate(bool gameplayView)
    {
        Vector3 enemyTo = gameplayView ? enemyBasePos + enemyHiddenOffset : enemyBasePos;
        Vector3 gameTo = gameplayView ? gameplayBasePos + gameplayShownOffset : gameplayBasePos;

        if (enemyTarget) SetPos(enemyTarget, enemyTo);
        if (gameplayTarget) SetPos(gameplayTarget, gameTo);
    }

    private Vector3 GetPos(Transform t)
    {
        if (!t) return Vector3.zero;
        return useLocalPosition ? t.localPosition : t.position;
    }

    private void SetPos(Transform t, Vector3 value)
    {
        if (!t) return;
        if (useLocalPosition) t.localPosition = value;
        else t.position = value;
    }

    private static float EvaluateEase(EaseType type, float x)
    {
        switch (type)
        {
            case EaseType.EaseInQuad: return x * x;
            case EaseType.EaseOutQuad: return 1f - (1f - x) * (1f - x);
            case EaseType.EaseInOutQuad:
                return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) * 0.5f;

            case EaseType.EaseInCubic: return x * x * x;
            case EaseType.EaseOutCubic: return 1f - Mathf.Pow(1f - x, 3f);
            case EaseType.EaseInOutCubic:
                return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f;

            default: return x;
        }
    }
}
