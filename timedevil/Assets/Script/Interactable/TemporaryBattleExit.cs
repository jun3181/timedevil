// Assets/Script/Interactable/TemporaryBattleExit.cs
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class TemporaryBattleExit : MonoBehaviour, IInteractable
{
    [Header("Fallback 설정")]
    [Tooltip("저장된 돌아갈 씬 정보가 없을 경우, 대신 이동할 씬 이름")]
    public string fallbackSceneName = "Mainmenu";
    [SerializeField] private bool useFaderIfExists = true;

    private bool isTransitioning = false;

    public void Interact()
    {
        if (isTransitioning) return;

        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        if (!HasBattleReturnRequest())
        {
            Debug.LogWarning("[TemporaryBattleExit] ReturnSceneName이 비어있어서 Fallback 씬으로 이동합니다.");
            StartCoroutine(ExitBattleSequence(fallbackSceneName, false));
            return;
        }

        StartCoroutine(ExitBattleSequence(null, true));
    }

    private IEnumerator ExitBattleSequence(string sceneName, bool useBattleReturn)
    {
        isTransitioning = true;
        BattleEncounterState.ClearPending();
        BattleVictoryReturnContext.ClearAll();

        // (선택) 입력 잠금
        if (GameManager.Instance != null)
            GameManager.Instance.isAction = true;

        if (useBattleReturn)
            SceneTransitionService.ReturnFromBattle(0f, useFaderIfExists: useFaderIfExists);
        else
            SceneTransitionService.LoadDefault(sceneName, useFaderIfExists: useFaderIfExists);

        // 씬 로드되면 이 오브젝트는 보통 파괴되므로 여기서 해제는 선택
        yield return null;
    }

    private bool HasBattleReturnRequest()
    {
        bool hasArrivalReturn =
            SceneArrivalContext.TryPeek(out SceneArrivalRequest request) &&
            request != null &&
            request.kind == SceneArrivalKind.BattleReturn &&
            !string.IsNullOrWhiteSpace(request.targetSceneName);

        return hasArrivalReturn || !string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName);
    }
}
