using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// 적 스톤을 4x4 그리드 안에서 상/하/좌/우로
/// 한 칸 "부드럽게" 이동시키는 컨트롤러.
/// EnemyController 가 ExecuteMoveOneStep()을 호출한다.
/// </summary>
public class EnemyMoveController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform enemyStone;     // 적 말 (필수)
    [SerializeField] private Transform gridOrigin;     // 그리드 (0,0) 월드 기준점

    [Header("Grid")]
    [SerializeField] private float tileSize = 1f;      // 칸 크기
    [SerializeField] private Vector2Int minGrid = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int maxGrid = new Vector2Int(3, 3);
    [SerializeField] private float zOverride = -2f;    // 항상 이 Z로 고정 표시

    [Header("Move Animation")]
    [SerializeField] private float moveDuration = 0.20f;          // 한 칸 이동 시간
    [SerializeField]
    private AnimationCurve moveCurve =            // 가속/감속 곡선
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    /// <summary>
    /// EnemyController 에서 호출. 가능한 방향 중 한 칸 랜덤 이동(애니메이션 포함).
    /// 이동 불가면 그냥 종료.
    /// </summary>
    public IEnumerator ExecuteMoveOneStep()
    {
        if (!enemyStone || !gridOrigin)
        {
            Debug.LogWarning("[EnemyMoveController] enemyStone/gridOrigin 미지정");
            yield break;
        }

        // 현재 그리드 좌표
        Vector2Int cur = WorldToGrid(enemyStone.position);
        // 후보 방향
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // 경계 안에 들어오는 타깃들만 필터
        var candidates = dirs
            .Select(d => cur + d)
            .Where(g => IsInside(g))
            .ToList();

        if (candidates.Count == 0)
        {
            // 이동 불가
            yield break;
        }

        // 랜덤 타깃
        Vector2Int target = candidates[Random.Range(0, candidates.Count)];

        // 월드 좌표 목적지
        Vector3 start = enemyStone.position;
        Vector3 end = GridToWorld(target);
        end.z = zOverride; // Z 고정

        // 애니메이션
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveDuration);
            float eased = moveCurve.Evaluate(k);
            enemyStone.position = Vector3.LerpUnclamped(start, end, eased);
            yield return null;
        }
        enemyStone.position = end;
    }

    // ----------------- helpers -----------------

    bool IsInside(Vector2Int g)
        => g.x >= minGrid.x && g.x <= maxGrid.x && g.y >= minGrid.y && g.y <= maxGrid.y;

    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - gridOrigin.position;
        return new Vector2Int(
            Mathf.RoundToInt(local.x / tileSize),
            Mathf.RoundToInt(local.y / tileSize)
        );
    }

    Vector3 GridToWorld(Vector2Int grid)
    {
        return gridOrigin.position + new Vector3(grid.x * tileSize, grid.y * tileSize, 0f);
    }

    /// <summary>씬 초기 배치가 어긋났을 때, 강제로 그리드에 스냅시키고 싶을 때 호출(선택).</summary>
    [ContextMenu("Snap Enemy To Grid")]
    void SnapEnemyToGrid()
    {
        if (!enemyStone || !gridOrigin) return;
        Vector2Int g = WorldToGrid(enemyStone.position);
        g.x = Mathf.Clamp(g.x, minGrid.x, maxGrid.x);
        g.y = Mathf.Clamp(g.y, minGrid.y, maxGrid.y);
        var p = GridToWorld(g);
        p.z = zOverride;
        enemyStone.position = p;
    }
}
