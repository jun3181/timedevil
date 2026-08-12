// Assets/Script/Interactable/TemporaryBattleExit.cs
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class TemporaryBattleExit : MonoBehaviour, IInteractable
{
    [Header("Fallback 설정")]
    [Tooltip("저장된 돌아갈 씬 정보가 없을 경우, 대신 이동할 씬 이름")]
    public string fallbackSceneName = "MainMenu";

    private bool isTransitioning = false;

    public void Interact()
    {
        if (isTransitioning) return;

        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        string sceneToLoad = PlayerReturnContext.ReturnSceneName;
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[TemporaryBattleExit] ReturnSceneName이 비어있어서 Fallback 씬으로 이동합니다.");
            sceneToLoad = fallbackSceneName;
        }

        StartCoroutine(ExitBattleSequence(sceneToLoad));
    }

    private IEnumerator ExitBattleSequence(string sceneName)
    {
        isTransitioning = true;
        BattleEncounterState.ClearPending();

        // (선택) 입력 잠금
        if (GameManager.Instance != null)
            GameManager.Instance.isAction = true;

        // 현재 씬에 있는 SceneFader 찾기
        var fader = FindObjectOfType<SceneFader>(true);
        if (fader != null)
        {
            // SceneFader 내부에서 FadeOut -> Load 수행
            fader.LoadSceneWithFadeOut(sceneName);
        }
        else
        {
            Debug.LogWarning("[TemporaryBattleExit] SceneFader를 찾지 못했습니다. 즉시 LoadScene 합니다.");
            SceneManager.LoadScene(sceneName);
        }

        // 씬 로드되면 이 오브젝트는 보통 파괴되므로 여기서 해제는 선택
        yield return null;
    }
}
