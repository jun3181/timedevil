using System.Collections;
using UnityEngine;

public class PlayerEvadeController : MonoBehaviour
{
    [Header("Target Pawn (실제 이동할 Transform)")]
    [SerializeField] private Transform playerPawn;

    [Header("Animator")]
    [SerializeField] private PlayerAnimeController animator;

    [Header("Grid Step (한 칸 크기)")]
    [SerializeField] private float stepX = 1.3f;
    [SerializeField] private float stepY = 1.3f;

    [Header("Allowed Tile Centers (Panel 경계)")]
    [Tooltip("플레이어 보드의 16개 센터(월드 좌표). GridOrigin/AttackController에서 쓰는 것과 동일하게 세팅")]
    [SerializeField] private Vector3[] allowedCenters = new Vector3[16];
    [SerializeField, Tooltip("도착 지점이 센터에 얼마나 가까워야 허용할지(월드 거리)")]
    private float snapEpsilon = 0.15f;

    [Header("Timing")]
    [SerializeField, Tooltip("편도 이동 시간(초)")]
    private float moveSeconds = 0.25f;

    private bool enemyAttackWindow = false;
    private bool evading = false; // 이동 중 입력 잠금

    void Awake()
    {
        if (!playerPawn) playerPawn = this.transform;
        if (!animator) animator = FindObjectOfType<PlayerAnimeController>(true);
        if (animator) animator.SetTarget(playerPawn);
    }

    void OnEnable() { EnemyTurnController.OnEnemyAttackWindowChanged += HandleEnemyAttackWindow; }
    void OnDisable() { EnemyTurnController.OnEnemyAttackWindowChanged -= HandleEnemyAttackWindow; }

    private void HandleEnemyAttackWindow(bool on) { enemyAttackWindow = on; }

    void Update()
    {
        if (!CanAcceptInput()) return;

        // 방향 입력
        Vector3 offset = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow)) offset = new Vector3(0f, stepY, 0f);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) offset = new Vector3(0f, -stepY, 0f);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) offset = new Vector3(-stepX, 0f, 0f);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) offset = new Vector3(stepX, 0f, 0f);
        else return;

        // 도착 후보 → 패널 내부인지 스냅 검사
        var cur = playerPawn.position;
        if (!TrySnapToAllowedCenter(cur + offset, out var snappedEnd))
            return; // 패널 밖이면 무시

        StartCoroutine(Co_MoveOnce(snappedEnd));
    }

    private bool CanAcceptInput()
    {
        if (evading) return false;
        var tm = TurnManager.Instance;
        if (tm == null || tm.currentTurn != TurnState.EnemyTurn) return false; // 적 턴에만 회피
        if (!enemyAttackWindow) return false;                                  // 공격 윈도우 중에만 회피
        return true;
    }

    /// <summary>
    /// 한 칸만 이동하고 끝(원위치 복귀 없음)
    /// </summary>
    private IEnumerator Co_MoveOnce(Vector3 endWorld)
    {
        evading = true;

        float dur = Mathf.Max(0.01f, moveSeconds);

        if (animator != null)
        {
            animator.AnimateTo(endWorld, dur);
            while (animator.IsPlaying) yield return null;
        }
        else
        {
            yield return LerpPosition(playerPawn.position, endWorld, dur);
        }

        // 스냅 보정(부동오차 방지)
        playerPawn.position = endWorld;

        evading = false;
    }

    // --- Allowed center check ---
    private bool TrySnapToAllowedCenter(Vector3 desired, out Vector3 snapped)
    {
        snapped = desired;
        if (allowedCenters == null || allowedCenters.Length == 0) return true; // 경계 미세팅 시 통과

        // 가장 가까운 센터 찾기
        float best = float.MaxValue;
        int bestIdx = -1;
        for (int i = 0; i < allowedCenters.Length; i++)
        {
            float d = Vector2.Distance((Vector2)desired, (Vector2)allowedCenters[i]);
            if (d < best) { best = d; bestIdx = i; }
        }

        if (bestIdx >= 0 && best <= snapEpsilon)
        {
            snapped = allowedCenters[bestIdx];
            return true; // 패널 내부로 인정
        }
        return false;     // 패널 탈출 → 이동 금지
    }

    // 폴백 보간(애니메이터 없을 때)
    private IEnumerator LerpPosition(Vector3 a, Vector3 b, float dur)
    {
        float t = 0f;
        dur = Mathf.Max(0.01f, dur);
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            playerPawn.position = Vector3.Lerp(a, b, u);
            yield return null;
        }
        playerPawn.position = b;
    }
}
