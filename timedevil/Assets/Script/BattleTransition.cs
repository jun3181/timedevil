using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요

/// <summary>
/// 문에 붙어서 배틀씬을 로드하고, 복귀 지점을 저장하는 스크립트.
/// IInteractable을 구현하여 E키 상호작용을 받습니다.
/// </summary>
public class BattleTransition : MonoBehaviour, IInteractable
{
    [Header("로드할 배틀씬")]
    [Tooltip("로드할 배틀씬의 이름 (빌드 세팅에 이름이 등록되어야 함)")]
    public string battleSceneName = "BattleScene"; // 예시 이름

    [Header("복귀 지점")]
    [Tooltip("배틀이 끝난 후, 이 씬으로 돌아왔을 때 플레이어가 나타날 위치")]
    public Transform returnPoint; // (기존 DoorTransition의 targetPoint 역할)

    private bool isTransitioning = false;

    /// <summary>
    /// 플레이어가 E키 등으로 호출하는 진입점
    /// </summary>
    public void Interact()
    {
        // 1. 설정이 안됐거나, 이미 전환 중이거나, 대화 중이면 무시
        if (string.IsNullOrEmpty(battleSceneName) || returnPoint == null)
        {
            Debug.LogWarning("[BattleTransition] 배틀씬 이름이나 복귀 지점이 설정되지 않았습니다.");
            return;
        }
        if (isTransitioning || (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive))
        {
            return;
        }

        // 2. 씬 전환 코루틴 시작
        StartCoroutine(StartBattleSequence());
    }

    IEnumerator StartBattleSequence()
    {
        isTransitioning = true;

        // 1. 플레이어 조작 비활성화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isAction = true;
        }

        // --- 2. (가장 중요) '돌아올 정보'를 정적 클래스에 저장 ---
        // 2a. 현재 씬의 이름을 저장
        PlayerReturnContext.ReturnSceneName = SceneManager.GetActiveScene().name;
        // 2b. 돌아올 좌표(문 앞)를 저장
        PlayerReturnContext.ReturnPosition = returnPoint.position;
        // 2c. 플래그를 켜서 '복귀 데이터 있음'을 표시
        PlayerReturnContext.HasReturnPosition = true;

        // 3. 화면 어둡게 (페이드 아웃)
        yield return StartCoroutine(SceneFader.instance.Fade(1f));

        // 4. 저장된 배틀씬으로 전환 (SceneFader의 기능 사용)
        SceneFader.instance.LoadSceneWithFade(battleSceneName);

        // (이 오브젝트는 씬이 전환되며 파괴되므로, isTransitioning을 false로 바꿀 필요 없음)
    }
}