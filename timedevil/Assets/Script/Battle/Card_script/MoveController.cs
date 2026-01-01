using System.Collections;
using UnityEngine;

public class MoveController : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int rows = 4;
    [SerializeField] private int cols = 4;
    [SerializeField] private float cell = 1.3f;

    // (r0,c0): origin이 가리키는 기준 셀(예: (4,1))
    [SerializeField] private int originRow_Player = 4;
    [SerializeField] private int originCol_Player = 1;
    [SerializeField] private int originRow_Enemy  = 4;
    [SerializeField] private int originCol_Enemy  = 1;

    [SerializeField] private Transform playerGridOrigin;
    [SerializeField] private Transform enemyGridOrigin;

    [Header("Actors")]
    [SerializeField] private Transform playerPawn;
    [SerializeField] private Transform enemyPawn;

    [Header("Runtime State (grid index)")]
    [SerializeField] private Vector2Int playerRC = new Vector2Int(4, 1); // (row, col)
    [SerializeField] private Vector2Int enemyRC  = new Vector2Int(2, 2);

    [Header("Animation")]
    [SerializeField] private float perCellSeconds = 0.15f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private string moveTriggerName = "Move";

    [Header("UI Lock")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private DescriptionPanelController desc;

    void Reset()
    {
        menu ??= FindObjectOfType<BattleMenuController>(true);
        desc ??= FindObjectOfType<DescriptionPanelController>(true);
    }

    public void SetGrid(Faction who, int r, int c, bool snap = true)
    {
        var rc = ClampRC(new Vector2Int(r, c));
        if (who == Faction.Player)
        {
            playerRC = rc;
            if (snap && playerPawn && playerGridOrigin)
                playerPawn.position = RCToWorld(rc, playerGridOrigin, originRow_Player, originCol_Player, playerPawn.position.z); // ★
        }
        else
        {
            enemyRC = rc;
            if (snap && enemyPawn && enemyGridOrigin)
                enemyPawn.position = RCToWorld(rc, enemyGridOrigin, originRow_Enemy, originCol_Enemy, enemyPawn.position.z);       // ★
        }
    }

    public Vector2Int GetGrid(Faction who) => (who == Faction.Player) ? playerRC : enemyRC;

    public IEnumerator Execute(MoveCardSO so, Faction self, Faction foe)
    {
        if (so == null) yield break;

        if (menu) menu.EnableInput(false);
        if (desc) desc.ShowTemporaryExplanation(
            string.IsNullOrEmpty(so.explanation)
                ? (string.IsNullOrEmpty(so.display) ? so.displayName : so.display)
                : so.explanation);

        var target = (so.moveMode == MoveMode.UpMove) ? self : foe;

        Transform origin = (target == Faction.Player) ? playerGridOrigin : enemyGridOrigin;
        int oRow = (target == Faction.Player) ? originRow_Player : originRow_Enemy;
        int oCol = (target == Faction.Player) ? originCol_Player : originCol_Enemy;

        Transform pawn = (target == Faction.Player) ? playerPawn : enemyPawn;
        Animator anim = (target == Faction.Player) ? playerAnimator : enemyAnimator;

        if (!pawn || !origin)
        {
            Debug.LogWarning("[MoveController] Pawn/Origin 누락");
            if (desc) desc.ClearTemporaryMessage();
            if (menu) menu.EnableInput(true);
            yield break;
        }

        Vector2Int curRC = GetGrid(target);
        Vector2Int deltaRC = DirToDelta(so.where) * Mathf.Max(0, so.amount);

        Vector3 startPos = pawn.position;
        Vector3 worldDelta = RCDeltaToWorldDelta(deltaRC, origin, oRow, oCol);
        Vector3 rawEndPos = startPos + worldDelta;

        var (minX, maxX, minY, maxY) = ComputeWorldBounds(origin, oRow, oCol);
        Vector3 clampedEndPos = new Vector3(
            Mathf.Clamp(rawEndPos.x, minX, maxX),
            Mathf.Clamp(rawEndPos.y, minY, maxY),
            startPos.z
        );

        Vector2Int endRC = WorldToNearestRC(clampedEndPos, origin, oRow, oCol);
        endRC = ClampRC(endRC);

        // ▶ 이동 칸 수 계산(애니/트윈 공용)
        int cellsDistance = Mathf.Abs(endRC.x - curRC.x) + Mathf.Abs(endRC.y - curRC.y);

        // ▶ 실제 이동 없으면 애니/트리거도 스킵
        if (cellsDistance == 0)
        {
            yield return new WaitForSeconds(0.05f);
            if (desc) desc.ClearTemporaryMessage();
            if (menu) menu.EnableInput(true);
            yield break;
        }

        // ▶ 애니 파라미터 세팅(변수 이름 충돌 방지)
        int animDir;
        if (endRC.y < curRC.y) animDir = 2; // Left
        else if (endRC.y > curRC.y) animDir = 3; // Right
        else if (endRC.x < curRC.x) animDir = 0; // Up
        else animDir = 1; // Down
        if (anim)
        {
            anim.SetInteger("Dir", animDir);
            anim.SetBool("Moving", true);

            float animDuration = Mathf.Max(0.001f, perCellSeconds * cellsDistance);
            anim.SetFloat("MoveSpeed", 1f / animDuration);
        }

        Vector3 endPos = RCToWorld(endRC, origin, oRow, oCol, startPos.z);

        if (anim && !string.IsNullOrEmpty(moveTriggerName))
            anim.SetTrigger(moveTriggerName);

        float tweenDuration = perCellSeconds * Mathf.Max(1, cellsDistance);

        float t = 0f;
        while (t < tweenDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / tweenDuration);
            float e = (ease != null) ? ease.Evaluate(u) : u;
            pawn.position = Vector3.LerpUnclamped(startPos, endPos, e);
            yield return null;
        }
        pawn.position = endPos;

        SetGrid(target, endRC.x, endRC.y, snap: false);

        if (anim) anim.SetBool("Moving", false);

        if (desc) desc.ClearTemporaryMessage();
        if (menu) menu.EnableInput(true);
    }


    // ===== 유틸 =====
    // 1) 유틸: Dir4 -> Animator Dir(Int) 매핑
    private static int DirToAnimInt(Dir4 d) => d switch
    {
        Dir4.Up => 0,
        Dir4.Down => 1,
        Dir4.Left => 2,
        Dir4.Right => 3,
        _ => 0
    };

    // 방향 → 그리드 델타 (부호 수정: Left -, Right +)
    private static Vector2Int DirToDelta(Dir4 d) => d switch
    {
        Dir4.Left  => new Vector2Int(0, -1),
        Dir4.Right => new Vector2Int(0, +1),
        Dir4.Up    => new Vector2Int(-1, 0),
        Dir4.Down  => new Vector2Int(+1, 0),
        _ => Vector2Int.zero
    };

    // 보드 월드 경계 계산(그리드 한계 → 월드 좌표)
    private (float minX, float maxX, float minY, float maxY) ComputeWorldBounds(Transform origin, int oRow, int oCol)
    {
        float minX = origin.position.x + (1    - oCol) * cell;
        float maxX = origin.position.x + (cols - oCol) * cell;
        float maxY = origin.position.y + (oRow - 1   ) * cell;   // row=1(윗줄)이 +Y 최대
        float minY = origin.position.y + (oRow - rows) * cell;   // row=rows(아랫줄)이 -Y 최소
        if (minX > maxX) (minX, maxX) = (maxX, minX);
        if (minY > maxY) (minY, maxY) = (maxY, minY);
        return (minX, maxX, minY, maxY);
    }

    // (r,c) → 월드 좌표
    private Vector3 RCToWorld(Vector2Int rc, Transform origin, int oRow, int oCol, float baseZ)
    {
        float x = origin.position.x + (rc.y - oCol) * cell;
        float y = origin.position.y + (oRow - rc.x) * cell;
        return new Vector3(x, y, baseZ);   // ← 전달받은 z를 그대로 사용
    }

    // 월드 델타 계산: 그리드 델타를 월드 벡터로 (Pawn 기준 상대 이동)
    private Vector3 RCDeltaToWorldDelta(Vector2Int dRC, Transform origin, int oRow, int oCol)
    {
        // 열(+1) → +X, 열(-1) → -X
        // 행(-1: Up) → +Y, 행(+1: Down) → -Y
        float dx = dRC.y * cell;
        float dy = (-dRC.x) * cell;
        return new Vector3(dx, dy, 0f);
    }

    // 월드 좌표 → 가장 가까운 그리드 (반올림)
    private Vector2Int WorldToNearestRC(Vector3 world, Transform origin, int oRow, int oCol)
    {
        float relX = (world.x - origin.position.x) / cell;
        float relY = (world.y - origin.position.y) / cell;

        int c = Mathf.RoundToInt(relX + oCol);
        int r = Mathf.RoundToInt(oRow - relY);

        return new Vector2Int(r, c);
    }

    private Vector2Int ClampRC(Vector2Int rc)
    {
        rc.x = Mathf.Clamp(rc.x, 1, rows);
        rc.y = Mathf.Clamp(rc.y, 1, cols);
        return rc;
    }
}
