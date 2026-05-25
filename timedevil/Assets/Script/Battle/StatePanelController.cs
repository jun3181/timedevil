using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class StatePanelController : MonoBehaviour
{
    [System.Serializable]
    private class StateTarget
    {
        public string label;
        public Faction faction;
        public Graphic highlightGraphic;
        public SpriteRenderer highlightSprite;
        public Color selectedColor = new Color(0.7f, 1f, 0.7f, 1f);
    }

    [Header("Refs")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private PanelController panelController;
    [SerializeField] private DescriptionPanelController descriptionPanel;
    [SerializeField] private PlayerDataRuntime playerRuntime;
    [SerializeField] private EnemyRuntime enemyRuntime;

    [Header("State Menu")]
    [SerializeField] private int stateIndex = 2;
    [SerializeField] private bool wrap = true;
    [SerializeField] private List<StateTarget> targets = new List<StateTarget>();

    private readonly List<Color> originalColors = new List<Color>();
    private readonly List<Color> originalSpriteColors = new List<Color>();
    private bool active;
    private bool menuHideRequested;
    private int currentIndex;

    void Reset()
    {
        ResolveRefs();
    }

    void Awake()
    {
        ResolveRefs();
        EnsureDefaultTargets();
        CacheOriginalColors();
    }

    void OnEnable()
    {
        if (menu) menu.onSubmit.AddListener(OnMenuSubmit);
    }

    void OnDisable()
    {
        if (menu) menu.onSubmit.RemoveListener(OnMenuSubmit);
        ExitStateMode(false);
    }

    void Update()
    {
        if (!active) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ExitStateMode(true);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
            MoveTarget(+1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            MoveTarget(-1);
    }

    private void OnMenuSubmit(int index)
    {
        if (active || index != ResolveStateIndex()) return;
        EnterStateMode();
    }

    private void EnterStateMode()
    {
        ResolveRefs();
        EnsureDefaultTargets();
        CacheOriginalColors();

        active = true;
        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, targets.Count - 1));

        if (menu) menu.EnableInput(false);
        if (panelController)
        {
            panelController.PushBattleMenuHideRequest();
            menuHideRequested = true;
            panelController.SetGameplayViewDelayed(true, 0.02f);
        }

        RefreshStateView();
    }

    private void ExitStateMode(bool restorePanelView)
    {
        if (!active) return;

        active = false;
        RestoreHighlights();
        descriptionPanel?.ExitStateView();
        ReleaseMenuHideRequest();

        if (menu) menu.EnableInput(true);
        if (restorePanelView && panelController) panelController.SetGameplayView(false);
    }

    private void ReleaseMenuHideRequest()
    {
        if (!menuHideRequested) return;
        menuHideRequested = false;
        if (panelController) panelController.PopBattleMenuHideRequest();
    }

    private void MoveTarget(int direction)
    {
        if (targets.Count == 0) return;

        int next = currentIndex + direction;
        if (wrap)
            next = (next % targets.Count + targets.Count) % targets.Count;
        else
            next = Mathf.Clamp(next, 0, targets.Count - 1);

        if (next == currentIndex) return;
        currentIndex = next;
        RefreshStateView();
    }

    private void RefreshStateView()
    {
        ApplyHighlights();
        descriptionPanel?.EnterStateView(BuildCurrentStateText());
    }

    private string BuildCurrentStateText()
    {
        if (targets.Count == 0)
            return "표시할 대상이 없습니다.";

        StateTarget target = targets[Mathf.Clamp(currentIndex, 0, targets.Count - 1)];
        string label = !string.IsNullOrWhiteSpace(target.label) ? target.label : target.faction.ToString();
        return target.faction == Faction.Player
            ? BuildPlayerStateText(label)
            : BuildEnemyStateText(label);
    }

    private string BuildPlayerStateText(string label)
    {
        var data = playerRuntime ? playerRuntime.Data : PlayerDataRuntime.Instance?.Data;
        if (data == null)
            return $"{label}\n상태 정보를 찾을 수 없습니다.";

        var sb = new StringBuilder();
        sb.AppendLine(label);
        sb.AppendLine($"HP : {Mathf.Max(0, data.currentHP)} / {Mathf.Max(1, data.maxHP)}");
        sb.AppendLine($"ATK : {data.attack}    DEF : {data.defense}    SPD : {data.speed}");
        sb.AppendLine($"Emotion : +{data.emotionPositive} / -{data.emotionNegative}");
        sb.Append("현재 플레이어의 전투 상태입니다.");
        return sb.ToString();
    }

    private string BuildEnemyStateText(string label)
    {
        var enemy = enemyRuntime ? enemyRuntime : EnemyRuntime.Instance;
        if (enemy == null)
            return $"{label}\n상태 정보를 찾을 수 없습니다.";

        var sb = new StringBuilder();
        sb.AppendLine(string.IsNullOrWhiteSpace(enemy.enemyName) ? label : enemy.enemyName);
        sb.AppendLine($"HP : {Mathf.Max(0, enemy.currentHP)} / {Mathf.Max(1, enemy.maxHP)}");
        sb.AppendLine($"ATK : {enemy.attack}    DEF : {enemy.defense}    SPD : {enemy.speed}");
        sb.Append("현재 적의 전투 상태입니다.");
        return sb.ToString();
    }

    private void ApplyHighlights()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var graphic = targets[i].highlightGraphic;
            var sprite = targets[i].highlightSprite;

            if (graphic)
            {
                Color original = i < originalColors.Count ? originalColors[i] : graphic.color;
                graphic.color = i == currentIndex ? targets[i].selectedColor : original;
            }

            if (sprite)
            {
                Color original = i < originalSpriteColors.Count ? originalSpriteColors[i] : sprite.color;
                sprite.color = i == currentIndex ? targets[i].selectedColor : original;
            }
        }
    }

    private void RestoreHighlights()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var graphic = targets[i].highlightGraphic;
            if (graphic && i < originalColors.Count)
                graphic.color = originalColors[i];

            var sprite = targets[i].highlightSprite;
            if (sprite && i < originalSpriteColors.Count)
                sprite.color = originalSpriteColors[i];
        }
    }

    private void CacheOriginalColors()
    {
        originalColors.Clear();
        originalSpriteColors.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            originalColors.Add(targets[i].highlightGraphic ? targets[i].highlightGraphic.color : Color.white);
            originalSpriteColors.Add(targets[i].highlightSprite ? targets[i].highlightSprite.color : Color.white);
        }
    }

    private int ResolveStateIndex()
    {
        return menu != null && menu.EntryCount >= 5 ? stateIndex : -1;
    }

    private void ResolveRefs()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!panelController) panelController = FindObjectOfType<PanelController>(true);
        if (!descriptionPanel) descriptionPanel = FindObjectOfType<DescriptionPanelController>(true);
        if (!playerRuntime) playerRuntime = PlayerDataRuntime.Instance ?? FindObjectOfType<PlayerDataRuntime>(true);
        if (!enemyRuntime) enemyRuntime = EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true);
    }

    private void EnsureDefaultTargets()
    {
        if (targets.Count > 0) return;

        targets.Add(new StateTarget { label = "Player", faction = Faction.Player });
        targets.Add(new StateTarget { label = "Enemy", faction = Faction.Enemy });
    }
}
