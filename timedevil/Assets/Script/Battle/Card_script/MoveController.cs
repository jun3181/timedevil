using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleTurnGridEmphasisMode
{
    CellFill,
    BoardOutline
}

public class MoveController : MonoBehaviour
{
    public event Action<Faction, Vector2Int> OnGridChanged;

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

    [Header("Grid Root Origin")]
    [SerializeField] private bool useGridObjectAsOrigin = true;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private bool autoFindBoardCenters = true;
    [SerializeField] private Transform playerBoardCenter;
    [SerializeField] private Transform enemyBoardCenter;
    [SerializeField] private Vector2 playerBoardCenterLocal = new Vector2(-4.325f, 0.155f);
    [SerializeField] private Vector2 enemyBoardCenterLocal = new Vector2(3.675f, 0.155f);

    [Header("Grid Scale")]
    [SerializeField] private bool useGridScaleForCell = true;
    [SerializeField] private Transform gridScaleReference;
    [SerializeField] private Transform playerGridScaleReference;
    [SerializeField] private Transform enemyGridScaleReference;
    private Transform cachedAutoGridScaleReference;
    private Transform cachedPlayerBoardCenter;
    private Transform cachedEnemyBoardCenter;

    [Header("Actors")]
    [SerializeField] private Transform playerPawn;
    [SerializeField] private Transform enemyPawn;
    [SerializeField] private bool keepPawnZ = true;
    [SerializeField] private float playerPawnZ = -2f;
    [SerializeField] private float enemyPawnZ = -2f;
    [SerializeField] private bool keepSortingOrder = true;
    [SerializeField] private int playerSortingOrder = 20;
    [SerializeField] private int enemySortingOrder = 20;

    [Header("Actor Visual Centering")]
    [SerializeField] private bool centerPlayerVisualBoundsOnCell = false;
    [SerializeField] private bool centerEnemyVisualBoundsOnCell = false;
    [SerializeField] private Vector2 playerVisualCenterOffset = Vector2.zero;
    [SerializeField] private Vector2 enemyVisualCenterOffset = Vector2.zero;

    [Header("Runtime State (grid index)")]
    [SerializeField] private Vector2Int playerRC = new Vector2Int(4, 1); // (row, col)
    [SerializeField] private Vector2Int enemyRC  = new Vector2Int(2, 2);

    [Header("Initial Grid Sync")]
    [SerializeField] private bool alignPawnsToStartGridOnStart = true;
    [SerializeField] private Vector2Int playerStartRC = new Vector2Int(4, 1);
    [SerializeField] private Vector2Int enemyStartRC = new Vector2Int(2, 2);
    [SerializeField] private bool syncGridFromPawnPositionsOnStart = false;
    [SerializeField] private bool snapPawnsToGridOnStart = true;

    [Header("Animation")]
    [SerializeField] private float perCellSeconds = 0.15f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private string chapter1RightWalkStateName = "Player_Right_Walk";

    [Header("UI Lock")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private DescriptionPanelController desc;

    [Header("Turn Grid Highlight")]
    [SerializeField] private bool highlightCurrentTurnGrid = true;
    [SerializeField] private BattleTurnGridEmphasisMode turnGridEmphasisMode = BattleTurnGridEmphasisMode.CellFill;
    [SerializeField] private Color turnGridHighlightColor = new Color(0.7f, 1f, 0.7f, 0.025f);
    [SerializeField, Range(0.1f, 1f)] private float turnGridHighlightScale = 0.92f;
    [SerializeField] private string turnGridHighlightSortingLayer = "Default";
    [SerializeField] private int turnGridHighlightSortingOrder = 1;
    [SerializeField] private float turnGridHighlightZ = -1f;
    [SerializeField] private Color turnGridOutlineColor = new Color(0.7f, 1f, 0.7f, 0.85f);
    [SerializeField, Min(0.001f)] private float turnGridOutlineWidth = 0.045f;
    [SerializeField] private Vector2 turnGridOutlinePadding = new Vector2(0.02f, 0.02f);
    [SerializeField] private int turnGridOutlineSortingOrder = 2;

    private bool _ownsTempMessage;
    private bool _initialGridAligned;
    private TurnManager turnHighlightTurnManager;
    private Transform turnGridHighlightRoot;
    private Sprite turnGridHighlightSprite;
    private LineRenderer playerTurnGridOutline;
    private LineRenderer enemyTurnGridOutline;
    private Material turnGridOutlineMaterial;
    private readonly List<SpriteRenderer> playerTurnGridHighlights = new List<SpriteRenderer>(16);
    private readonly List<SpriteRenderer> enemyTurnGridHighlights = new List<SpriteRenderer>(16);
    private readonly Vector3[] turnHighlightCenters = new Vector3[16];

    void Reset()
    {
        menu ??= FindObjectOfType<BattleMenuController>(true);
        desc ??= FindObjectOfType<DescriptionPanelController>(true);
    }

    void Awake()
    {
        ForceAlignInitialPawnsToGrid();
    }

    void OnEnable()
    {
        BindTurnHighlightManager();
    }

    void OnDisable()
    {
        if (turnHighlightTurnManager != null)
            turnHighlightTurnManager.OnTurnChanged -= HandleTurnHighlightChanged;
        turnHighlightTurnManager = null;
        SetTurnGridHighlightsVisible(playerTurnGridHighlights, false);
        SetTurnGridHighlightsVisible(enemyTurnGridHighlights, false);
        SetTurnGridOutlineVisible(playerTurnGridOutline, false);
        SetTurnGridOutlineVisible(enemyTurnGridOutline, false);
    }

    void OnDestroy()
    {
        if (turnGridOutlineMaterial)
            Destroy(turnGridOutlineMaterial);
    }

    void Start()
    {
        ForceAlignInitialPawnsToGrid();
        StartCoroutine(Co_AlignInitialPawnsAfterFirstFrame());
        BindTurnHighlightManager();
        RefreshTurnGridHighlight();
    }

    void LateUpdate()
    {
        if (keepSortingOrder)
        {
            ApplySorting(playerPawn, playerSortingOrder);
            ApplySorting(enemyPawn, enemySortingOrder);
        }

        if (highlightCurrentTurnGrid)
            RefreshTurnGridHighlightPositions();
    }

    public void SetGrid(Faction who, int r, int c, bool snap = true)
    {
        var rc = ClampRC(new Vector2Int(r, c));
        if (who == Faction.Player)
        {
            playerRC = rc;
            if (snap && playerPawn && HasCoordinateSource(who))
                playerPawn.position = GetPawnPositionForCell(who, rc, keepPawnZ ? playerPawnZ : playerPawn.position.z);
            ApplySorting(playerPawn, playerSortingOrder);
        }
        else
        {
            enemyRC = rc;
            if (snap && enemyPawn && HasCoordinateSource(who))
                enemyPawn.position = GetPawnPositionForCell(who, rc, keepPawnZ ? enemyPawnZ : enemyPawn.position.z);
            ApplySorting(enemyPawn, enemySortingOrder);
        }

        OnGridChanged?.Invoke(who, rc);
    }

    public Vector2Int GetGrid(Faction who) => (who == Faction.Player) ? playerRC : enemyRC;

    public Transform GetPawn(Faction who) => (who == Faction.Player) ? playerPawn : enemyPawn;

    public Vector2 GetCellSize(Faction who)
    {
        Transform root = GetGridRoot();
        if (useGridObjectAsOrigin && root)
            return MeasureRootCellSize(root);

        Transform origin = GetGridOrigin(who);
        return GetCellStep(origin, who);
    }

    public Vector3 GridToWorld(Faction who, Vector2Int rc, float z)
    {
        Transform root = GetGridRoot();
        if (useGridObjectAsOrigin && root)
            return GridRootRCToWorld(who, ClampRC(rc), root, z);

        Transform origin = GetGridOrigin(who);
        if (!origin) return Vector3.positiveInfinity;

        return RCToWorld(ClampRC(rc), origin, GetOriginRow(who), GetOriginCol(who), GetCellStep(origin, who), z);
    }

    public Vector2Int WorldToGrid(Faction who, Vector3 world)
    {
        Transform root = GetGridRoot();
        if (useGridObjectAsOrigin && root)
            return ClampRC(GridRootWorldToRC(who, world, root));

        Transform origin = GetGridOrigin(who);
        if (!origin) return GetGrid(who);

        return ClampRC(WorldToNearestRC(world, origin, GetOriginRow(who), GetOriginCol(who), GetCellStep(origin, who)));
    }

    public bool TryBuildPatternCenters(Faction who, Vector3[] centers16, float z)
    {
        if (centers16 == null || centers16.Length < 16) return false;

        if (!HasCoordinateSource(who)) return false;

        for (int i = 0; i < 16; i++)
            centers16[i] = GridToWorld(who, PatternIndexToRC(i), z);

        return true;
    }

    public void AlignInitialPawnsToGrid()
    {
        if (_initialGridAligned) return;

        ForceAlignInitialPawnsToGrid();
    }

    public void ForceAlignInitialPawnsToGrid()
    {
        bool playerAligned = SyncInitialPawnWithGrid(Faction.Player);
        bool enemyAligned = SyncInitialPawnWithGrid(Faction.Enemy);
        _initialGridAligned = playerAligned && enemyAligned;
    }

    private IEnumerator Co_AlignInitialPawnsAfterFirstFrame()
    {
        yield return null;
        ForceAlignInitialPawnsToGrid();
        RefreshTurnGridHighlightPositions();
    }

    public IEnumerator Execute(MoveCardSO so, Faction self, Faction foe)
    {
        if (so == null) yield break;

        if (menu) menu.EnableInput(false);
        _ownsTempMessage = false;
        if (desc && !desc.HasForcedMessage)
        {
            _ownsTempMessage = true;
            desc.ShowTemporaryExplanation(
                string.IsNullOrEmpty(so.explanation)
                    ? (string.IsNullOrEmpty(so.display) ? so.displayName : so.display)
                    : so.explanation);
        }

        var target = (so.moveMode == MoveMode.UpMove) ? self : foe;

        Transform pawn = (target == Faction.Player) ? playerPawn : enemyPawn;
        Animator anim = (target == Faction.Player) ? playerAnimator : enemyAnimator;

        if (!pawn || !HasCoordinateSource(target))
        {
            Debug.LogWarning("[MoveController] Pawn/Origin 누락");
            if (_ownsTempMessage && desc) desc.ClearTemporaryMessage();
            if (menu) menu.EnableInput(true);
            yield break;
        }

        ApplySorting(pawn, target == Faction.Player ? playerSortingOrder : enemySortingOrder);

        Vector2Int curRC = GetGrid(target);
        Vector2Int deltaRC = DirToDelta(so.where) * Mathf.Max(0, so.amount);
        Vector2Int endRC = ClampRC(curRC + deltaRC);

        Vector3 startPos = pawn.position;
        if (keepPawnZ)
            startPos.z = (target == Faction.Player) ? playerPawnZ : enemyPawnZ;

        // ▶ 이동 칸 수 계산(애니/트윈 공용)
        int cellsDistance = Mathf.Abs(endRC.x - curRC.x) + Mathf.Abs(endRC.y - curRC.y);

        // ▶ 실제 이동 없으면 애니/트리거도 스킵
        if (cellsDistance == 0)
        {
            yield return new WaitForSeconds(0.05f);
            if (_ownsTempMessage && desc) desc.ClearTemporaryMessage();
            if (menu) menu.EnableInput(true);
            yield break;
        }

        // Play chapter1's right-walk animation only while this move tween is running.
        BattleMoveAnimatorSnapshot animSnapshot = BeginMoveAnimation(anim);

        Vector3 endPos = GetPawnPositionForCell(target, endRC, keepPawnZ ? ((target == Faction.Player) ? playerPawnZ : enemyPawnZ) : startPos.z);

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

        EndMoveAnimation(anim, animSnapshot);

        if (_ownsTempMessage && desc) desc.ClearTemporaryMessage();
        if (menu) menu.EnableInput(true);
    }


    // ===== 유틸 =====
    // 방향 → 그리드 델타 (부호 수정: Left -, Right +)
    private static Vector2Int DirToDelta(Dir4 d) => d switch
    {
        Dir4.Left  => new Vector2Int(0, -1),
        Dir4.Right => new Vector2Int(0, +1),
        Dir4.Up    => new Vector2Int(-1, 0),
        Dir4.Down  => new Vector2Int(+1, 0),
        _ => Vector2Int.zero
    };

    private bool SyncInitialPawnWithGrid(Faction who)
    {
        Transform pawn = GetPawn(who);
        if (!pawn || !HasCoordinateSource(who)) return false;

        Vector2Int rc = GetGrid(who);
        if (syncGridFromPawnPositionsOnStart)
        {
            rc = WorldToGrid(who, pawn.position);
        }
        else if (alignPawnsToStartGridOnStart)
        {
            rc = GetStartGrid(who);
        }

        if (syncGridFromPawnPositionsOnStart || alignPawnsToStartGridOnStart || snapPawnsToGridOnStart)
            SetGrid(who, rc.x, rc.y, snapPawnsToGridOnStart);

        return true;
    }

    private Vector3 GetPawnPositionForCell(Faction who, Vector2Int rc, float z)
    {
        Vector3 cellCenter = GridToWorld(who, rc, z);
        Transform pawn = GetPawn(who);
        if (!pawn || !ShouldCenterVisualBoundsOnCell(who))
            return cellCenter;

        Vector3 visualOffset = GetPawnVisualBoundsCenterOffset(pawn);
        Vector2 manualOffset = who == Faction.Player ? playerVisualCenterOffset : enemyVisualCenterOffset;
        Vector3 target = cellCenter - new Vector3(visualOffset.x, visualOffset.y, 0f);
        target += new Vector3(manualOffset.x, manualOffset.y, 0f);
        target.z = z;
        return target;
    }

    private bool ShouldCenterVisualBoundsOnCell(Faction who)
    {
        return who == Faction.Player ? centerPlayerVisualBoundsOnCell : centerEnemyVisualBoundsOnCell;
    }

    private Vector3 GetPawnVisualBoundsCenterOffset(Transform pawn)
    {
        if (!TryGetPawnVisualBounds(pawn, out Bounds bounds))
            return Vector3.zero;

        return bounds.center - pawn.position;
    }

    private bool TryGetPawnVisualBounds(Transform pawn, out Bounds bounds)
    {
        bounds = default;
        if (!pawn)
            return false;

        bool hasBounds = false;
        SpriteRenderer[] sprites = pawn.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sr = sprites[i];
            if (!sr || !sr.enabled || !sr.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                bounds = sr.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        if (hasBounds)
            return true;

        Renderer[] renderers = pawn.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void BindTurnHighlightManager()
    {
        var manager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (manager == turnHighlightTurnManager) return;

        if (turnHighlightTurnManager != null)
            turnHighlightTurnManager.OnTurnChanged -= HandleTurnHighlightChanged;

        turnHighlightTurnManager = manager;
        if (turnHighlightTurnManager != null)
            turnHighlightTurnManager.OnTurnChanged += HandleTurnHighlightChanged;
    }

    private void HandleTurnHighlightChanged(TurnState _)
    {
        RefreshTurnGridHighlight();
    }

    private void RefreshTurnGridHighlight()
    {
        BindTurnHighlightManager();

        bool canShow = highlightCurrentTurnGrid
            && turnHighlightTurnManager != null
            && turnHighlightTurnManager.HasFirstTurnDecided;

        if (!canShow)
        {
            SetTurnGridHighlightsVisible(playerTurnGridHighlights, false);
            SetTurnGridHighlightsVisible(enemyTurnGridHighlights, false);
            SetTurnGridOutlineVisible(playerTurnGridOutline, false);
            SetTurnGridOutlineVisible(enemyTurnGridOutline, false);
            return;
        }

        bool playerTurn = turnHighlightTurnManager.currentTurn == TurnState.PlayerTurn;
        if (turnGridEmphasisMode == BattleTurnGridEmphasisMode.BoardOutline)
        {
            SetTurnGridHighlightsVisible(playerTurnGridHighlights, false);
            SetTurnGridHighlightsVisible(enemyTurnGridHighlights, false);
            EnsureTurnGridOutlines();
            UpdateTurnGridOutline(Faction.Player, playerTurnGridOutline, playerTurn);
            UpdateTurnGridOutline(Faction.Enemy, enemyTurnGridOutline, !playerTurn);
            return;
        }

        SetTurnGridOutlineVisible(playerTurnGridOutline, false);
        SetTurnGridOutlineVisible(enemyTurnGridOutline, false);
        EnsureTurnGridHighlights();
        SetTurnGridHighlightsVisible(playerTurnGridHighlights, playerTurn);
        SetTurnGridHighlightsVisible(enemyTurnGridHighlights, !playerTurn);
        RefreshTurnGridHighlightPositions();
    }

    private void RefreshTurnGridHighlightPositions()
    {
        if (!highlightCurrentTurnGrid || turnHighlightTurnManager == null || !turnHighlightTurnManager.HasFirstTurnDecided)
            return;

        bool playerTurn = turnHighlightTurnManager.currentTurn == TurnState.PlayerTurn;
        if (turnGridEmphasisMode == BattleTurnGridEmphasisMode.BoardOutline)
        {
            EnsureTurnGridOutlines();
            UpdateTurnGridOutline(Faction.Player, playerTurnGridOutline, playerTurn);
            UpdateTurnGridOutline(Faction.Enemy, enemyTurnGridOutline, !playerTurn);
            return;
        }

        EnsureTurnGridHighlights();
        UpdateTurnGridHighlightSide(Faction.Player, playerTurnGridHighlights, playerTurn);
        UpdateTurnGridHighlightSide(Faction.Enemy, enemyTurnGridHighlights, !playerTurn);
    }

    private void EnsureTurnGridHighlights()
    {
        if (!turnGridHighlightSprite)
        {
            turnGridHighlightSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        if (!turnGridHighlightRoot)
        {
            var go = new GameObject("TurnGridHighlights");
            turnGridHighlightRoot = go.transform;
            turnGridHighlightRoot.SetParent(transform, worldPositionStays: true);
        }

        EnsureTurnGridHighlightSide(Faction.Player, playerTurnGridHighlights);
        EnsureTurnGridHighlightSide(Faction.Enemy, enemyTurnGridHighlights);
    }

    private void EnsureTurnGridHighlightSide(Faction side, List<SpriteRenderer> renderers)
    {
        int need = Mathf.Max(1, rows) * Mathf.Max(1, cols);
        while (renderers.Count < need)
        {
            var go = new GameObject($"{side}TurnGridHighlight_{renderers.Count:D2}");
            go.transform.SetParent(turnGridHighlightRoot, worldPositionStays: true);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = turnGridHighlightSprite;
            sr.sortingLayerName = turnGridHighlightSortingLayer;
            sr.sortingOrder = turnGridHighlightSortingOrder;
            sr.color = turnGridHighlightColor;
            go.SetActive(false);
            renderers.Add(sr);
        }
    }

    private void UpdateTurnGridHighlightSide(Faction side, List<SpriteRenderer> renderers, bool visible)
    {
        if (!visible || !TryBuildPatternCenters(side, turnHighlightCenters, turnGridHighlightZ))
        {
            SetTurnGridHighlightsVisible(renderers, false);
            return;
        }

        Vector2 size = GetCellSize(side);
        int n = Mathf.Min(renderers.Count, Mathf.Max(1, rows) * Mathf.Max(1, cols));
        for (int i = 0; i < n; i++)
        {
            var sr = renderers[i];
            if (!sr) continue;

            sr.transform.position = turnHighlightCenters[i];
            float scale = Mathf.Clamp(turnGridHighlightScale, 0.1f, 1f);
            sr.transform.localScale = new Vector3(size.x * scale, size.y * scale, 1f);
            sr.sortingLayerName = turnGridHighlightSortingLayer;
            sr.sortingOrder = turnGridHighlightSortingOrder;
            sr.color = turnGridHighlightColor;
            sr.gameObject.SetActive(true);
        }
    }

    private void SetTurnGridHighlightsVisible(List<SpriteRenderer> renderers, bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i]) renderers[i].gameObject.SetActive(visible);
    }

    private void EnsureTurnGridOutlines()
    {
        if (!playerTurnGridOutline)
            playerTurnGridOutline = CreateTurnGridOutline("PlayerTurnGridOutline");
        if (!enemyTurnGridOutline)
            enemyTurnGridOutline = CreateTurnGridOutline("EnemyTurnGridOutline");
    }

    private LineRenderer CreateTurnGridOutline(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, worldPositionStays: true);

        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 5;
        line.loop = false;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.sharedMaterial = GetTurnGridOutlineMaterial();
        line.enabled = false;
        return line;
    }

    private void UpdateTurnGridOutline(Faction side, LineRenderer line, bool visible)
    {
        if (!line)
            return;

        if (!visible || !TryGetTurnBoardBounds(side, out Bounds bounds))
        {
            SetTurnGridOutlineVisible(line, false);
            return;
        }

        float padX = Mathf.Max(0f, turnGridOutlinePadding.x);
        float padY = Mathf.Max(0f, turnGridOutlinePadding.y);
        bounds.Expand(new Vector3(padX * 2f, padY * 2f, 0f));

        float z = turnGridHighlightZ;
        line.positionCount = 5;
        line.SetPosition(0, new Vector3(bounds.min.x, bounds.min.y, z));
        line.SetPosition(1, new Vector3(bounds.max.x, bounds.min.y, z));
        line.SetPosition(2, new Vector3(bounds.max.x, bounds.max.y, z));
        line.SetPosition(3, new Vector3(bounds.min.x, bounds.max.y, z));
        line.SetPosition(4, new Vector3(bounds.min.x, bounds.min.y, z));
        line.startWidth = turnGridOutlineWidth;
        line.endWidth = turnGridOutlineWidth;
        line.startColor = turnGridOutlineColor;
        line.endColor = turnGridOutlineColor;
        line.sortingLayerName = turnGridHighlightSortingLayer;
        line.sortingOrder = turnGridOutlineSortingOrder;
        line.sharedMaterial = GetTurnGridOutlineMaterial();
        SetTurnGridOutlineVisible(line, true);
    }

    private bool TryGetTurnBoardBounds(Faction side, out Bounds bounds)
    {
        Transform board = GetAutoBoardCenter(side);
        if (board)
        {
            Renderer renderer = board.GetComponent<Renderer>() ?? board.GetComponentInChildren<Renderer>(true);
            if (renderer)
            {
                bounds = renderer.bounds;
                return true;
            }
        }

        if (TryBuildPatternCenters(side, turnHighlightCenters, turnGridHighlightZ))
        {
            Vector2 cellSize = GetCellSize(side);
            Vector3 min = turnHighlightCenters[0];
            Vector3 max = turnHighlightCenters[0];
            int n = Mathf.Min(turnHighlightCenters.Length, Mathf.Max(1, rows) * Mathf.Max(1, cols));
            for (int i = 1; i < n; i++)
            {
                min = Vector3.Min(min, turnHighlightCenters[i]);
                max = Vector3.Max(max, turnHighlightCenters[i]);
            }

            Vector3 size = max - min + new Vector3(cellSize.x, cellSize.y, 0f);
            Vector3 center = (min + max) * 0.5f;
            bounds = new Bounds(center, size);
            return true;
        }

        bounds = default;
        return false;
    }

    private void SetTurnGridOutlineVisible(LineRenderer line, bool visible)
    {
        if (line)
            line.enabled = visible;
    }

    private Material GetTurnGridOutlineMaterial()
    {
        if (turnGridOutlineMaterial)
            return turnGridOutlineMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (!shader) shader = Shader.Find("UI/Default");
        if (shader) turnGridOutlineMaterial = new Material(shader);

        return turnGridOutlineMaterial;
    }

    private Transform GetGridOrigin(Faction who)
    {
        return who == Faction.Player ? playerGridOrigin : enemyGridOrigin;
    }

    private int GetOriginRow(Faction who)
    {
        return who == Faction.Player ? originRow_Player : originRow_Enemy;
    }

    private int GetOriginCol(Faction who)
    {
        return who == Faction.Player ? originCol_Player : originCol_Enemy;
    }

    private Vector2Int GetStartGrid(Faction who)
    {
        return ClampRC(who == Faction.Player ? playerStartRC : enemyStartRC);
    }

    private Vector2Int PatternIndexToRC(int index)
    {
        int safeCols = Mathf.Max(1, cols);
        int safeIndex = Mathf.Max(0, index);
        int r = (safeIndex / safeCols) + 1;
        int c = (safeIndex % safeCols) + 1;
        return ClampRC(new Vector2Int(r, c));
    }

    private bool HasCoordinateSource(Faction who)
    {
        if (useGridObjectAsOrigin && GetGridRoot()) return true;
        return GetGridOrigin(who) != null;
    }

    private Transform GetGridRoot()
    {
        if (gridRoot) return gridRoot;
        if (cachedAutoGridScaleReference) return cachedAutoGridScaleReference;

        Grid[] grids = FindObjectsOfType<Grid>(true);
        for (int i = 0; i < grids.Length; i++)
        {
            if (grids[i] != null && grids[i].name == "Grid")
            {
                cachedAutoGridScaleReference = grids[i].transform;
                return cachedAutoGridScaleReference;
            }
        }

        if (grids.Length == 1 && grids[0] != null)
            cachedAutoGridScaleReference = grids[0].transform;

        return cachedAutoGridScaleReference;
    }

    private Vector3 GridRootRCToWorld(Faction who, Vector2Int rc, Transform root, float z)
    {
        Vector2 center = GetBoardCenterLocal(who);
        float centerCol = (cols + 1) * 0.5f;
        float centerRow = (rows + 1) * 0.5f;
        float safeCell = Mathf.Max(0.0001f, cell);
        Vector3 local = new Vector3(
            center.x + (rc.y - centerCol) * safeCell,
            center.y + (centerRow - rc.x) * safeCell,
            0f
        );

        Vector3 world = root.TransformPoint(local);
        world.z = z;
        return world;
    }

    private Vector2Int GridRootWorldToRC(Faction who, Vector3 world, Transform root)
    {
        Vector3 local = root.InverseTransformPoint(world);
        Vector2 center = GetBoardCenterLocal(who);
        float centerCol = (cols + 1) * 0.5f;
        float centerRow = (rows + 1) * 0.5f;
        float safeCell = Mathf.Max(0.0001f, cell);

        int c = Mathf.RoundToInt(((local.x - center.x) / safeCell) + centerCol);
        int r = Mathf.RoundToInt(centerRow - ((local.y - center.y) / safeCell));
        return new Vector2Int(r, c);
    }

    private Vector2 GetBoardCenterLocal(Faction who)
    {
        Transform explicitCenter = who == Faction.Player ? playerBoardCenter : enemyBoardCenter;
        if (explicitCenter)
            return ToGridRootLocal(explicitCenter);

        if (autoFindBoardCenters)
        {
            Transform found = GetAutoBoardCenter(who);
            if (found)
                return ToGridRootLocal(found);
        }

        return who == Faction.Player ? playerBoardCenterLocal : enemyBoardCenterLocal;
    }

    private Vector2 ToGridRootLocal(Transform target)
    {
        Transform root = GetGridRoot();
        Vector3 local = root ? root.InverseTransformPoint(target.position) : target.localPosition;
        return new Vector2(local.x, local.y);
    }

    private Transform GetAutoBoardCenter(Faction who)
    {
        if (who == Faction.Player && cachedPlayerBoardCenter) return cachedPlayerBoardCenter;
        if (who == Faction.Enemy && cachedEnemyBoardCenter) return cachedEnemyBoardCenter;

        Transform root = GetGridRoot();
        if (!root) return null;

        string expectedName = who == Faction.Player ? "Square" : "Square (1)";
        Transform found = root.Find(expectedName);
        if (!found)
            found = FindExtremeBoardSprite(root, who);

        if (who == Faction.Player)
            cachedPlayerBoardCenter = found;
        else
            cachedEnemyBoardCenter = found;

        return found;
    }

    private Transform FindExtremeBoardSprite(Transform root, Faction who)
    {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        Transform best = null;
        float bestX = who == Faction.Player ? float.PositiveInfinity : float.NegativeInfinity;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (!sr || sr.transform == root) continue;

            Vector3 local = root.InverseTransformPoint(sr.transform.position);
            if (who == Faction.Player)
            {
                if (local.x < bestX)
                {
                    bestX = local.x;
                    best = sr.transform;
                }
            }
            else if (local.x > bestX)
            {
                bestX = local.x;
                best = sr.transform;
            }
        }

        return best;
    }

    private Vector2 MeasureRootCellSize(Transform root)
    {
        float safeCell = Mathf.Max(0.0001f, cell);
        Vector3 origin = root.TransformPoint(Vector3.zero);
        Vector3 right = root.TransformPoint(new Vector3(safeCell, 0f, 0f));
        Vector3 up = root.TransformPoint(new Vector3(0f, safeCell, 0f));

        return new Vector2(
            Mathf.Max(0.0001f, Vector3.Distance(origin, right)),
            Mathf.Max(0.0001f, Vector3.Distance(origin, up))
        );
    }

    // 보드 월드 경계 계산(그리드 한계 → 월드 좌표)
    private (float minX, float maxX, float minY, float maxY) ComputeWorldBounds(Transform origin, int oRow, int oCol, Vector2 cellStep)
    {
        float minX = origin.position.x + (1    - oCol) * cellStep.x;
        float maxX = origin.position.x + (cols - oCol) * cellStep.x;
        float maxY = origin.position.y + (oRow - 1   ) * cellStep.y;   // row=1(윗줄)이 +Y 최대
        float minY = origin.position.y + (oRow - rows) * cellStep.y;   // row=rows(아랫줄)이 -Y 최소
        if (minX > maxX) (minX, maxX) = (maxX, minX);
        if (minY > maxY) (minY, maxY) = (maxY, minY);
        return (minX, maxX, minY, maxY);
    }

    // (r,c) → 월드 좌표
    private Vector3 RCToWorld(Vector2Int rc, Transform origin, int oRow, int oCol, Vector2 cellStep, float baseZ)
    {
        float x = origin.position.x + (rc.y - oCol) * cellStep.x;
        float y = origin.position.y + (oRow - rc.x) * cellStep.y;
        return new Vector3(x, y, baseZ);   // ← 전달받은 z를 그대로 사용
    }

    // 월드 델타 계산: 그리드 델타를 월드 벡터로 (Pawn 기준 상대 이동)
    private Vector3 RCDeltaToWorldDelta(Vector2Int dRC, Vector2 cellStep)
    {
        // 열(+1) → +X, 열(-1) → -X
        // 행(-1: Up) → +Y, 행(+1: Down) → -Y
        float dx = dRC.y * cellStep.x;
        float dy = (-dRC.x) * cellStep.y;
        return new Vector3(dx, dy, 0f);
    }

    // 월드 좌표 → 가장 가까운 그리드 (반올림)
    private Vector2Int WorldToNearestRC(Vector3 world, Transform origin, int oRow, int oCol, Vector2 cellStep)
    {
        float relX = (world.x - origin.position.x) / cellStep.x;
        float relY = (world.y - origin.position.y) / cellStep.y;

        int c = Mathf.RoundToInt(relX + oCol);
        int r = Mathf.RoundToInt(oRow - relY);

        return new Vector2Int(r, c);
    }

    private Vector2 GetCellStep(Transform origin, Faction target)
    {
        float baseCell = Mathf.Max(0.0001f, cell);
        if (!useGridScaleForCell)
            return new Vector2(baseCell, baseCell);

        Transform reference = GetGridScaleReference(target);
        if (!reference) reference = origin;
        if (!reference)
            return new Vector2(baseCell, baseCell);

        Vector3 scale = reference.lossyScale;
        float sx = Mathf.Abs(scale.x);
        float sy = Mathf.Abs(scale.y);
        if (sx <= 0.0001f) sx = 1f;
        if (sy <= 0.0001f) sy = 1f;

        return new Vector2(baseCell * sx, baseCell * sy);
    }

    private Transform GetGridScaleReference(Faction target)
    {
        Transform specific = target == Faction.Player ? playerGridScaleReference : enemyGridScaleReference;
        if (specific) return specific;
        if (gridScaleReference) return gridScaleReference;
        if (useGridObjectAsOrigin && gridRoot) return gridRoot;
        if (cachedAutoGridScaleReference) return cachedAutoGridScaleReference;

        return GetGridRoot();
    }

    private Vector2Int ClampRC(Vector2Int rc)
    {
        rc.x = Mathf.Clamp(rc.x, 1, rows);
        rc.y = Mathf.Clamp(rc.y, 1, cols);
        return rc;
    }

    private void ApplySorting(Transform pawn, int order)
    {
        if (!keepSortingOrder || pawn == null) return;
        var srs = pawn.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
            srs[i].sortingOrder = order;
    }

    private BattleMoveAnimatorSnapshot BeginMoveAnimation(Animator anim)
    {
        var snapshot = new BattleMoveAnimatorSnapshot(anim);
        if (!anim) return snapshot;

        anim.enabled = true;
        SetAnimatorIntegerIfExists(anim, "hAxisRaw", 1);
        SetAnimatorIntegerIfExists(anim, "vAxisRaw", 0);
        SetAnimatorBoolIfExists(anim, "isChange", true);

        if (!string.IsNullOrEmpty(chapter1RightWalkStateName))
            anim.Play(chapter1RightWalkStateName, 0, 0f);

        anim.Update(0f);

        return snapshot;
    }

    private void EndMoveAnimation(Animator anim, BattleMoveAnimatorSnapshot snapshot)
    {
        if (!anim) return;

        SetAnimatorBoolIfExists(anim, "isChange", false);

        if (snapshot.HasHAxisRaw)
            anim.SetInteger("hAxisRaw", snapshot.HAxisRaw);

        if (snapshot.HasVAxisRaw)
            anim.SetInteger("vAxisRaw", snapshot.VAxisRaw);

        anim.enabled = snapshot.AnimatorWasEnabled;

        snapshot.RestoreSprites();
    }

    private static void SetAnimatorBoolIfExists(Animator anim, string parameterName, bool value)
    {
        if (HasAnimatorParameter(anim, parameterName, AnimatorControllerParameterType.Bool))
            anim.SetBool(parameterName, value);
    }

    private static void SetAnimatorIntegerIfExists(Animator anim, string parameterName, int value)
    {
        if (HasAnimatorParameter(anim, parameterName, AnimatorControllerParameterType.Int))
            anim.SetInteger(parameterName, value);
    }

    private static bool HasAnimatorParameter(Animator anim, string parameterName, AnimatorControllerParameterType type)
    {
        if (anim == null || string.IsNullOrEmpty(parameterName)) return false;

        var parameters = anim.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.type == type && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private readonly struct BattleMoveAnimatorSnapshot
    {
        public readonly bool AnimatorWasEnabled;
        public readonly bool HasHAxisRaw;
        public readonly int HAxisRaw;
        public readonly bool HasVAxisRaw;
        public readonly int VAxisRaw;
        private readonly SpriteRenderer[] spriteRenderers;
        private readonly Sprite[] sprites;

        public BattleMoveAnimatorSnapshot(Animator anim)
        {
            AnimatorWasEnabled = anim != null && anim.enabled;
            HasHAxisRaw = HasAnimatorParameter(anim, "hAxisRaw", AnimatorControllerParameterType.Int);
            HAxisRaw = HasHAxisRaw ? anim.GetInteger("hAxisRaw") : 0;
            HasVAxisRaw = HasAnimatorParameter(anim, "vAxisRaw", AnimatorControllerParameterType.Int);
            VAxisRaw = HasVAxisRaw ? anim.GetInteger("vAxisRaw") : 0;

            spriteRenderers = anim ? anim.GetComponentsInChildren<SpriteRenderer>(true) : null;
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                sprites = null;
                return;
            }

            sprites = new Sprite[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                sprites[i] = spriteRenderers[i] ? spriteRenderers[i].sprite : null;
        }

        public void RestoreSprites()
        {
            if (spriteRenderers == null || sprites == null) return;

            int count = Mathf.Min(spriteRenderers.Length, sprites.Length);
            for (int i = 0; i < count; i++)
            {
                if (spriteRenderers[i])
                    spriteRenderers[i].sprite = sprites[i];
            }
        }
    }
}
