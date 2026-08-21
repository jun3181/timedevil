using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class BattleTutorialActionPrompt
{
    [TextArea(2, 6)]
    public string message = "";

    public Vector2 windowAnchoredPosition = new Vector2(0f, 180f);
    public Vector2 windowSize = new Vector2(760f, 180f);
    public BattleTutorialAction requiredAction = BattleTutorialAction.None;
    public bool allowMenuNavigation = true;
    public bool allowCardSelectionNavigation = true;
    public bool allowStateNavigation = true;
    public bool allowCancelInput = true;
}

public enum BattleTutorialScenarioStepType
{
    Dialogue,
    Prompt,
    SetMenuInput,
    StartEnemyTurn,
    StartPlayerTurn,
    WaitForTurn,
    WaitForCardUseToSettle,
    MarkSeen,
    InfoPrompt
}

internal enum BattleTutorialEnemyCardRole
{
    Attack,
    Move,
    Draw
}

[System.Serializable]
public class BattleTutorialScenarioStep
{
    public string label = "";
    public BattleTutorialScenarioStepType type = BattleTutorialScenarioStepType.Prompt;

    [FormerlySerializedAs("customDialogue")]
    public BattleDialogue dialogue = new BattleDialogue();

    [FormerlySerializedAs("customPrompt")]
    public BattleTutorialActionPrompt prompt = new BattleTutorialActionPrompt();

    public TurnState turnToWaitFor = TurnState.PlayerTurn;
    public bool menuInputEnabled = true;
}

public class BattleTutorialScenarioController : MonoBehaviour
{
    public static BattleTutorialScenarioController Instance { get; private set; }
    private const int CurrentScenarioDefinitionVersion = 2;

    [Header("Flow")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool playOnlyOnce = false;
    [SerializeField] private string playerPrefsKey = "BattleTutorialScenario_Seen";
    [SerializeField] private bool controlBattleStart = true;

    [Header("Refs")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BattleDialogueController dialogueController;
    [SerializeField] private BattleTutorialController tutorialController;
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private CardUseOrchestrator cardUseOrchestrator;
    [SerializeField] private EnemyHandUI enemyHandUI;
    [SerializeField] private CardDatabaseSO cardDatabase;

    [Header("Enemy Card Role Tutorial")]
    [SerializeField] private bool explainEnemyCardRolesBeforeFirstEnemyPlay = true;
    [SerializeField] private BattleTutorialActionPrompt enemyAttackCardPrompt = new BattleTutorialActionPrompt
    {
        message = "공격 카드는 상대에게 피해를 주는 카드입니다. 적이 공격 카드를 사용하면 곧바로 공격 효과가 처리됩니다.",
        windowAnchoredPosition = new Vector2(0f, 245f),
        windowSize = new Vector2(820f, 180f),
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };
    [SerializeField] private BattleTutorialActionPrompt enemyMoveCardPrompt = new BattleTutorialActionPrompt
    {
        message = "이동 카드는 사용자의 위치를 바꿉니다. 적이 움직이면 다음 공격 위치도 달라질 수 있습니다.",
        windowAnchoredPosition = new Vector2(0f, 245f),
        windowSize = new Vector2(820f, 180f),
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };
    [SerializeField] private BattleTutorialActionPrompt enemyDrawCardPrompt = new BattleTutorialActionPrompt
    {
        message = "드로우 카드는 손패를 보충합니다. 손패가 늘어나면 이어서 사용할 선택지도 많아집니다.",
        windowAnchoredPosition = new Vector2(0f, 245f),
        windowSize = new Vector2(820f, 180f),
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };

    [Header("Scenario Steps")]
    [SerializeField] private List<BattleTutorialScenarioStep> scenarioSteps = new List<BattleTutorialScenarioStep>();
    [SerializeField, HideInInspector] private bool scenarioStepsInitialized;
    [SerializeField, HideInInspector] private int scenarioDefinitionVersion;

    [SerializeField, HideInInspector] private BattleDialogue introDialogue = new BattleDialogue
    {
        defaultSpeakerName = "Luke",
        lines = new[]
        {
            new BattleDialogueLine { text = "저게... 뭐야?" },
            new BattleDialogueLine { text = "이쪽으로 오고 있어...!" },
        }
    };

    [SerializeField, HideInInspector] private BattleDialogue afterFirstEnemyTurnDialogue = new BattleDialogue
    {
        defaultSpeakerName = "Luke",
        lines = new[]
        {
            new BattleDialogueLine { text = "먼저 상황을 파악해야겠어." },
        }
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt battleFlowPrompt = new BattleTutorialActionPrompt
    {
        message = "이 전투는 플레이어 턴부터 시작합니다. 턴마다 행동을 확인하고, 할 일을 마치면 End로 턴을 넘깁니다.",
        windowAnchoredPosition = new Vector2(0f, 245f),
        windowSize = new Vector2(820f, 180f),
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt deckRulePrompt = new BattleTutorialActionPrompt
    {
        message = "카드 덱이 비어 있으면 기본 카드 10장이 덱에 들어옵니다. 배틀 중 화면에 들고 있는 손패는 최대 3장까지입니다.",
        windowAnchoredPosition = new Vector2(0f, 245f),
        windowSize = new Vector2(820f, 180f),
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };

    [SerializeField, HideInInspector] private BattleDialogue beforeCardUseDialogue = new BattleDialogue
    {
        defaultSpeakerName = "Luke",
        lines = new[]
        {
            new BattleDialogueLine { text = "손패를 봤어. 이 카드들로 행동할 수 있는 거구나." },
            new BattleDialogueLine { text = "좋아... 아무 카드라도 써서 반격해보자." },
        }
    };

    [SerializeField, HideInInspector] private BattleDialogue beforeEndTurnDialogue = new BattleDialogue
    {
        defaultSpeakerName = "Luke",
        lines = new[]
        {
            new BattleDialogueLine { text = "이제 더 이상 할 게 없는데..." },
            new BattleDialogueLine { speakerName = "???", text = "행동을 마쳤다면 턴을 넘겨." },
        }
    };

    [SerializeField, HideInInspector] private BattleDialogue beforeRunDialogue = new BattleDialogue
    {
        defaultSpeakerName = "Luke",
        lines = new[]
        {
            new BattleDialogueLine { text = "여긴 너무 위험해..." },
            new BattleDialogueLine { text = "지금은 도망쳐야겠어." },
        }
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt statePanelPrompt = new BattleTutorialActionPrompt
    {
        message = "좌우 방향키로 State 패널을 선택하고 E로 상태창을 여세요.",
        requiredAction = BattleTutorialAction.StatePanelInteract,
        allowMenuNavigation = true,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt stateHandInspectPrompt = new BattleTutorialActionPrompt
    {
        message = "상태창에서는 적과 아군을 확인할 수 있습니다. E를 눌러 선택한 대상의 손패를 크게 확인하세요.",
        requiredAction = BattleTutorialAction.StateHandInspect,
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = true,
        allowCancelInput = false,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt closeStateHandPrompt = new BattleTutorialActionPrompt
    {
        message = "손패 확인을 마쳤다면 Q로 확대된 손패를 닫으세요.",
        requiredAction = BattleTutorialAction.StateCancel,
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = true,
        allowCancelInput = true,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt closeStatePanelPrompt = new BattleTutorialActionPrompt
    {
        message = "한 번 더 Q를 눌러 State 패널을 닫고 전투 메뉴로 돌아가세요.",
        requiredAction = BattleTutorialAction.StateCancel,
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = true,
        allowCancelInput = true,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt cardPanelPrompt = new BattleTutorialActionPrompt
    {
        message = "Card 패널을 선택하고 E를 눌러 손패를 펼치세요.",
        windowAnchoredPosition = new Vector2(-360f, 210f),
        windowSize = new Vector2(760f, 170f),
        requiredAction = BattleTutorialAction.CardPanelInteract,
        allowMenuNavigation = true,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt cardSelectPrompt = new BattleTutorialActionPrompt
    {
        message = "손패가 보이면 E를 한 번 더 눌러 카드 선택 상태로 들어가세요.",
        windowAnchoredPosition = new Vector2(-360f, 300f),
        windowSize = new Vector2(760f, 170f),
        requiredAction = BattleTutorialAction.CardSelect,
        allowMenuNavigation = false,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = false,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt cardUsePrompt = new BattleTutorialActionPrompt
    {
        message = "좌우 방향키로 사용할 카드를 고른 뒤 E로 사용하세요.",
        windowAnchoredPosition = new Vector2(-360f, 300f),
        windowSize = new Vector2(760f, 170f),
        requiredAction = BattleTutorialAction.CardUse,
        allowMenuNavigation = true,
        allowCardSelectionNavigation = true,
        allowStateNavigation = false,
        allowCancelInput = true,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt endTurnPrompt = new BattleTutorialActionPrompt
    {
        message = "손패 선택 중이라면 Q로 닫고, End 패널을 선택한 뒤 E로 턴을 종료하세요.",
        requiredAction = BattleTutorialAction.TurnEnd,
        allowMenuNavigation = true,
        allowCardSelectionNavigation = true,
        allowStateNavigation = false,
        allowCancelInput = true,
    };

    [SerializeField, HideInInspector] private BattleTutorialActionPrompt runPrompt = new BattleTutorialActionPrompt
    {
        message = "Run 패널을 선택하고 E를 눌러 전투에서 도망치세요.",
        requiredAction = BattleTutorialAction.RunPanelInteract,
        allowMenuNavigation = true,
        allowCardSelectionNavigation = false,
        allowStateNavigation = false,
        allowCancelInput = true,
    };

    private bool running;
    private bool enemyCardRolePromptsPlayed;

    public bool ShouldControlBattleStart => autoStart && controlBattleStart && isActiveAndEnabled && ShouldPlayNow();

    void Reset()
    {
        PopulateDefaultScenarioSteps();
    }

    void OnValidate()
    {
        EnsureScenarioStepsInitialized();
    }

    void Awake()
    {
        Instance = this;
        EnsureScenarioStepsInitialized();
        ResolveRefs();
    }

    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public void BeginControlledBattleStart(TurnManager manager)
    {
        if (running)
            return;

        if (manager)
            turnManager = manager;

        StartCoroutine(CoRunScenario());
    }

    public void PlayScenarioNow()
    {
        if (running)
            return;

        ResolveRefs();
        StartCoroutine(CoRunScenario());
    }

    private IEnumerator CoRunScenario()
    {
        running = true;
        enemyCardRolePromptsPlayed = false;
        EnsureScenarioStepsInitialized();
        ResolveRefs();

        foreach (BattleTutorialScenarioStep step in scenarioSteps)
            yield return ExecuteStep(step);

        MarkSeen();
        running = false;
    }

    private IEnumerator ExecuteStep(BattleTutorialScenarioStep step)
    {
        if (step == null)
            yield break;

        switch (step.type)
        {
            case BattleTutorialScenarioStepType.Dialogue:
                yield return PlayDialogueAndWait(step.dialogue);
                break;

            case BattleTutorialScenarioStepType.Prompt:
                yield return ShowPromptAndWait(step.prompt);
                break;

            case BattleTutorialScenarioStepType.InfoPrompt:
                yield return ShowInfoPromptAndWait(step.prompt);
                break;

            case BattleTutorialScenarioStepType.SetMenuInput:
                if (menu)
                    menu.EnableInput(step.menuInputEnabled);
                break;

            case BattleTutorialScenarioStepType.StartEnemyTurn:
                if (turnManager)
                    turnManager.BeginEnemyTurn();
                break;

            case BattleTutorialScenarioStepType.StartPlayerTurn:
                if (turnManager)
                    turnManager.BeginPlayerTurn();
                break;

            case BattleTutorialScenarioStepType.WaitForTurn:
                yield return WaitForTurn(step.turnToWaitFor);
                break;

            case BattleTutorialScenarioStepType.WaitForCardUseToSettle:
                yield return WaitForCardUseToSettle();
                break;

            case BattleTutorialScenarioStepType.MarkSeen:
                MarkSeen();
                break;
        }
    }

    private void EnsureScenarioStepsInitialized()
    {
        if (NeedsScenarioDefinitionMigration())
        {
            PopulateDefaultScenarioSteps();
            return;
        }

        if (scenarioStepsInitialized)
            return;

        if (NeedsDefaultScenarioSteps())
            PopulateDefaultScenarioSteps();
        else
        {
            scenarioStepsInitialized = true;
            scenarioDefinitionVersion = CurrentScenarioDefinitionVersion;
        }
    }

    private void PopulateDefaultScenarioSteps()
    {
        scenarioSteps = CreateDefaultScenarioSteps();
        scenarioStepsInitialized = true;
        scenarioDefinitionVersion = CurrentScenarioDefinitionVersion;
    }

    private bool NeedsScenarioDefinitionMigration()
    {
        if (scenarioDefinitionVersion >= CurrentScenarioDefinitionVersion)
            return false;

        if (scenarioSteps == null || scenarioSteps.Count == 0)
            return true;

        foreach (BattleTutorialScenarioStep step in scenarioSteps)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.label))
                continue;

            if (step.label == "Battle card rules dialogue"
                || step.label == "After first enemy turn dialogue")
                return true;
        }

        return false;
    }

    private bool NeedsDefaultScenarioSteps()
    {
        if (scenarioSteps == null || scenarioSteps.Count == 0)
            return true;

        bool hasContentStep = false;
        foreach (BattleTutorialScenarioStep step in scenarioSteps)
        {
            if (step == null)
                continue;

            if (step.type == BattleTutorialScenarioStepType.Dialogue)
            {
                hasContentStep = true;
                if (HasDialogue(step.dialogue))
                    return false;
            }

            if (step.type == BattleTutorialScenarioStepType.Prompt)
            {
                hasContentStep = true;
                if (step.prompt != null && !string.IsNullOrWhiteSpace(step.prompt.message))
                    return false;
            }
        }

        return hasContentStep;
    }

    private List<BattleTutorialScenarioStep> CreateDefaultScenarioSteps()
    {
        return new List<BattleTutorialScenarioStep>
        {
            MenuStep("Lock input for intro", false),
            DialogueStep("Intro dialogue", introDialogue),
            SimpleStep("Start player turn", BattleTutorialScenarioStepType.StartPlayerTurn),
            WaitTurnStep("Wait for first player turn", TurnState.PlayerTurn),
            MenuStep("Lock input before first explanation", false),
            InfoPromptStep("Battle flow prompt", battleFlowPrompt),
            InfoPromptStep("Deck rule prompt", deckRulePrompt),
            MenuStep("Enable state panel input", true),
            PromptStep("State panel prompt", statePanelPrompt),
            PromptStep("State hand inspect prompt", stateHandInspectPrompt),
            PromptStep("Close state hand prompt", closeStateHandPrompt),
            PromptStep("Close state panel prompt", closeStatePanelPrompt),
            MenuStep("Lock input before end turn dialogue", false),
            DialogueStep("Before end turn dialogue", beforeEndTurnDialogue),
            MenuStep("Enable end turn input", true),
            PromptStep("End turn prompt", endTurnPrompt),
            WaitTurnStep("Wait for second enemy turn", TurnState.EnemyTurn),
            WaitTurnStep("Wait for second player turn", TurnState.PlayerTurn),
            MenuStep("Lock input before run dialogue", false),
            DialogueStep("Before run dialogue", beforeRunDialogue),
            MenuStep("Enable run input", true),
            PromptStep("Run prompt", runPrompt),
            SimpleStep("Mark tutorial seen", BattleTutorialScenarioStepType.MarkSeen),
        };
    }

    private static BattleTutorialScenarioStep DialogueStep(string label, BattleDialogue dialogue)
    {
        return new BattleTutorialScenarioStep
        {
            label = label,
            type = BattleTutorialScenarioStepType.Dialogue,
            dialogue = CloneDialogue(dialogue)
        };
    }

    private static BattleTutorialScenarioStep PromptStep(string label, BattleTutorialActionPrompt prompt)
    {
        return new BattleTutorialScenarioStep
        {
            label = label,
            type = BattleTutorialScenarioStepType.Prompt,
            prompt = ClonePrompt(prompt)
        };
    }

    private static BattleTutorialScenarioStep InfoPromptStep(string label, BattleTutorialActionPrompt prompt)
    {
        return new BattleTutorialScenarioStep
        {
            label = label,
            type = BattleTutorialScenarioStepType.InfoPrompt,
            prompt = ClonePrompt(prompt)
        };
    }

    private static BattleTutorialScenarioStep MenuStep(string label, bool enabled)
    {
        return new BattleTutorialScenarioStep
        {
            label = label,
            type = BattleTutorialScenarioStepType.SetMenuInput,
            menuInputEnabled = enabled
        };
    }

    private static BattleTutorialScenarioStep WaitTurnStep(string label, TurnState turn)
    {
        return new BattleTutorialScenarioStep
        {
            label = label,
            type = BattleTutorialScenarioStepType.WaitForTurn,
            turnToWaitFor = turn
        };
    }

    private static BattleTutorialScenarioStep SimpleStep(string label, BattleTutorialScenarioStepType type)
    {
        return new BattleTutorialScenarioStep
        {
            label = label,
            type = type
        };
    }

    private IEnumerator PlayDialogueAndWait(BattleDialogue dialogue)
    {
        if (dialogueController == null)
            ResolveRefs();

        if (dialogueController == null || !HasDialogue(dialogue))
            yield break;

        dialogueController.Play(dialogue);
        while (dialogueController != null && dialogueController.IsDialogueActive)
            yield return null;
    }

    private IEnumerator ShowPromptAndWait(BattleTutorialActionPrompt prompt)
    {
        if (prompt == null || string.IsNullOrWhiteSpace(prompt.message))
            yield break;

        tutorialController = ResolveActiveTutorialController();

        if (tutorialController == null)
            yield break;

        bool shown = tutorialController.ShowExternalPrompt(
            prompt.message,
            prompt.windowAnchoredPosition,
            prompt.windowSize,
            BattleTutorialAdvanceMode.WaitAction,
            prompt.requiredAction,
            prompt.allowMenuNavigation,
            prompt.allowCardSelectionNavigation,
            prompt.allowStateNavigation,
            prompt.allowCancelInput);

        if (!shown)
            yield break;

        while (tutorialController != null && tutorialController.IsExternalPromptActive)
            yield return null;
    }

    private IEnumerator ShowInfoPromptAndWait(BattleTutorialActionPrompt prompt)
    {
        if (prompt == null || string.IsNullOrWhiteSpace(prompt.message))
            yield break;

        tutorialController = ResolveActiveTutorialController();

        if (tutorialController == null)
            yield break;

        bool shown = tutorialController.ShowExternalPrompt(
            prompt.message,
            prompt.windowAnchoredPosition,
            prompt.windowSize,
            BattleTutorialAdvanceMode.PressE,
            BattleTutorialAction.Continue,
            false,
            false,
            false,
            false);

        if (!shown)
            yield break;

        while (tutorialController != null && tutorialController.IsExternalPromptActive)
            yield return null;
    }

    public IEnumerator PlayEnemyCardRolePromptsBeforeFirstPlay(IReadOnlyList<string> currentEnemyHandIds)
    {
        if (!running || !explainEnemyCardRolesBeforeFirstEnemyPlay || enemyCardRolePromptsPlayed)
            yield break;

        enemyCardRolePromptsPlayed = true;
        ResolveRefs();

        if (enemyHandUI != null)
        {
            enemyHandUI.gameObject.SetActive(true);
            enemyHandUI.RebuildFromHand();
            yield return null;
        }

        yield return ShowEnemyCardRolePrompt(BattleTutorialEnemyCardRole.Attack, enemyAttackCardPrompt, currentEnemyHandIds);
        yield return ShowEnemyCardRolePrompt(BattleTutorialEnemyCardRole.Move, enemyMoveCardPrompt, currentEnemyHandIds);
        yield return ShowEnemyCardRolePrompt(BattleTutorialEnemyCardRole.Draw, enemyDrawCardPrompt, currentEnemyHandIds);

        if (enemyHandUI != null)
            enemyHandUI.ExitSelectMode();
    }

    private IEnumerator ShowEnemyCardRolePrompt(
        BattleTutorialEnemyCardRole role,
        BattleTutorialActionPrompt prompt,
        IReadOnlyList<string> fallbackEnemyHandIds)
    {
        if (prompt == null || string.IsNullOrWhiteSpace(prompt.message))
            yield break;

        int index = FindEnemyCardRoleIndex(role, fallbackEnemyHandIds);
        if (enemyHandUI != null && index >= 0)
            enemyHandUI.EnterReadOnlySelectMode(index, true);

        yield return ShowInfoPromptAndWait(prompt);
    }

    private int FindEnemyCardRoleIndex(BattleTutorialEnemyCardRole role, IReadOnlyList<string> fallbackEnemyHandIds)
    {
        if (enemyHandUI != null && enemyHandUI.CardCount > 0)
        {
            for (int i = 0; i < enemyHandUI.CardCount; i++)
            {
                string id = enemyHandUI.GetVisibleIdAt(i);
                if (IsEnemyCardRole(id, role))
                    return i;
            }
        }

        if (fallbackEnemyHandIds == null)
            return -1;

        for (int i = 0; i < fallbackEnemyHandIds.Count; i++)
        {
            if (IsEnemyCardRole(fallbackEnemyHandIds[i], role))
                return i;
        }

        return -1;
    }

    private bool IsEnemyCardRole(string id, BattleTutorialEnemyCardRole role)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        EnsureCardDatabase();
        BaseCardSO card = cardDatabase ? cardDatabase.GetById(id) : null;
        if (card != null)
        {
            if (role == BattleTutorialEnemyCardRole.Attack) return card is AttackCardSO;
            if (role == BattleTutorialEnemyCardRole.Move) return card is MoveCardSO;
            if (role == BattleTutorialEnemyCardRole.Draw) return card is DrawCardSO;
        }

        return role switch
        {
            BattleTutorialEnemyCardRole.Attack => id.StartsWith("AttackCard"),
            BattleTutorialEnemyCardRole.Move => id.StartsWith("MoveCard"),
            BattleTutorialEnemyCardRole.Draw => id.StartsWith("DrawCard"),
            _ => false
        };
    }

    private IEnumerator WaitForTurn(TurnState expectedTurn)
    {
        while (turnManager == null)
        {
            ResolveRefs();
            yield return null;
        }

        while (turnManager != null && turnManager.currentTurn != expectedTurn)
            yield return null;
    }

    private IEnumerator WaitForCardUseToSettle()
    {
        while (cardUseOrchestrator != null && cardUseOrchestrator.GetIsBusy())
            yield return null;

        yield return null;
    }

    private bool ShouldPlayNow()
    {
        return !playOnlyOnce
            || string.IsNullOrEmpty(playerPrefsKey)
            || PlayerPrefs.GetInt(playerPrefsKey, 0) == 0;
    }

    private void MarkSeen()
    {
        if (!playOnlyOnce || string.IsNullOrEmpty(playerPrefsKey))
            return;

        PlayerPrefs.SetInt(playerPrefsKey, 1);
        PlayerPrefs.Save();
    }

    private void ResolveRefs()
    {
        if (!turnManager)
            turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (!dialogueController)
            dialogueController = BattleDialogueController.Instance ?? FindObjectOfType<BattleDialogueController>(true);
        tutorialController = ResolveActiveTutorialController();
        if (!menu)
            menu = FindObjectOfType<BattleMenuController>(true);
        if (!cardUseOrchestrator)
            cardUseOrchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (!enemyHandUI)
            enemyHandUI = FindObjectOfType<EnemyHandUI>(true);
        EnsureCardDatabase();
    }

    private void EnsureCardDatabase()
    {
        if (cardDatabase)
            return;

        if (!cardUseOrchestrator)
            cardUseOrchestrator = FindObjectOfType<CardUseOrchestrator>(true);

        if (cardUseOrchestrator && cardUseOrchestrator.CardDatabase)
            cardDatabase = cardUseOrchestrator.CardDatabase;

        if (!cardDatabase)
            cardDatabase = Resources.Load<CardDatabaseSO>("CardDatabase");
    }

    private BattleTutorialController ResolveActiveTutorialController()
    {
        if (tutorialController != null && tutorialController.isActiveAndEnabled)
            return tutorialController;

        BattleTutorialController instance = BattleTutorialController.Instance;
        if (instance != null && instance.isActiveAndEnabled)
            return instance;

        BattleTutorialController active = FindObjectOfType<BattleTutorialController>();
        if (active != null && active.isActiveAndEnabled)
            return active;

        var go = new GameObject("BattleTutorialController_Runtime");
        return go.AddComponent<BattleTutorialController>();
    }

    private static bool HasDialogue(BattleDialogue dialogue)
    {
        return dialogue != null && dialogue.lines != null && dialogue.lines.Length > 0;
    }

    private static BattleDialogue CloneDialogue(BattleDialogue source)
    {
        if (source == null)
            return new BattleDialogue { lines = new BattleDialogueLine[0] };

        var clone = new BattleDialogue
        {
            defaultSpeakerName = source.defaultSpeakerName,
            defaultLeftPortrait = source.defaultLeftPortrait,
            defaultRightPortrait = source.defaultRightPortrait,
            lines = new BattleDialogueLine[source.lines != null ? source.lines.Length : 0]
        };

        if (source.lines == null)
            return clone;

        for (int i = 0; i < source.lines.Length; i++)
            clone.lines[i] = CloneLine(source.lines[i]);

        return clone;
    }

    private static BattleDialogueLine CloneLine(BattleDialogueLine source)
    {
        if (source == null)
            return new BattleDialogueLine();

        return new BattleDialogueLine
        {
            text = source.text,
            speakerName = source.speakerName,
            leftPortrait = source.leftPortrait,
            rightPortrait = source.rightPortrait,
            focus = source.focus,
            lineTutorial = CloneDialogueTutorialPrompt(source.lineTutorial),
            advanceTutorial = CloneDialogueTutorialPrompt(source.advanceTutorial)
        };
    }

    private static BattleDialogueTutorialPrompt CloneDialogueTutorialPrompt(BattleDialogueTutorialPrompt source)
    {
        if (source == null)
            return new BattleDialogueTutorialPrompt();

        return new BattleDialogueTutorialPrompt
        {
            enabled = source.enabled,
            message = source.message,
            windowAnchoredPosition = source.windowAnchoredPosition,
            windowSize = source.windowSize
        };
    }

    private static BattleTutorialActionPrompt ClonePrompt(BattleTutorialActionPrompt source)
    {
        if (source == null)
            return new BattleTutorialActionPrompt();

        return new BattleTutorialActionPrompt
        {
            message = source.message,
            windowAnchoredPosition = source.windowAnchoredPosition,
            windowSize = source.windowSize,
            requiredAction = source.requiredAction,
            allowMenuNavigation = source.allowMenuNavigation,
            allowCardSelectionNavigation = source.allowCardSelectionNavigation,
            allowStateNavigation = source.allowStateNavigation,
            allowCancelInput = source.allowCancelInput
        };
    }
}
