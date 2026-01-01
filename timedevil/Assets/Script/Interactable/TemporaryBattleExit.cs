using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요

/// <summary>
/// '배틀 씬'(임시 씬)에서 특정 사물과 상호작용(E키)했을 때,
/// PlayerReturnContext에 저장된 '원래 씬'으로 돌아가게 해주는 스크립트.
/// </summary>
public class TemporaryBattleExit : MonoBehaviour, IInteractable
{
    [Header("Fallback 설정")]
    [Tooltip("만약 저장된 돌아갈 씬 정보가 없을 경우, 대신 이동할 씬 이름")]
    public string fallbackSceneName = "MainMenu"; // (예: 메인메뉴)

    private bool isTransitioning = false;

    /// <summary>
    /// 플레이어가 E키 등으로 호출하는 진입점
    /// </summary>
    public void Interact()
    {
        // 1. 중복 실행 방지
        if (isTransitioning) return;

        // 2. (선택 사항) 대화가 진행 중이면 상호작용 무시
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
        {
            return;
        }

        // 3. (중요) UndeadMover가 저장해 둔 '돌아갈 씬 이름'을 가져옴
        string sceneToLoad = PlayerReturnContext.ReturnSceneName;

        // 4. (안전 장치) 저장된 씬 이름이 없다면(예: 배틀씬부터 테스트)
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[TemporaryBattleExit] 돌아갈 씬(PlayerReturnContext)이 저장되지 않았습니다. Fallback 씬으로 이동합니다.");
            sceneToLoad = fallbackSceneName;
        }

        // 5. 씬 전환 코루틴 시작
        StartCoroutine(ExitBattleSequence(sceneToLoad));
    }

    IEnumerator ExitBattleSequence(string sceneName)
    {
        isTransitioning = true;

        // 1. 플레이어 조작 비활성화 (선택 사항이지만, 페이드 중 움직임을 막음)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isAction = true;
        }

        // 2. (수정된) SceneFader를 이용해 씬 전환
        // (SceneFader가 페이드아웃/로드/페이드인을 모두 알아서 담당)
        if (SceneFader.instance != null)
        {
            SceneFader.instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            Debug.LogError("[TemporaryBattleExit] SceneFader.instance가 null입니다! 씬 전환 실패.");
            isTransitioning = false;
        }

        yield return null; // 코루틴 유지를 위해
    }
}