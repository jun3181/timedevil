// Assets/Script/BattleTransition.cs
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BattleTransition : MonoBehaviour, IInteractable
{
    [Header("로드할 배틀씬")]
    [Tooltip("빌드 세팅에 등록된 씬 이름")]
    public string battleSceneName = "BattleScene";

    [Header("복귀 지점")]
    [Tooltip("배틀 종료 후 이 씬으로 돌아왔을 때 플레이어가 나타날 위치")]
    public Transform returnPoint;

    private bool isTransitioning = false;

    public void Interact()
    {
        if (string.IsNullOrEmpty(battleSceneName) || returnPoint == null)
        {
            Debug.LogWarning("[BattleTransition] battleSceneName 또는 returnPoint가 설정되지 않았습니다.");
            return;
        }

        if (isTransitioning) return;
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive) return;

        StartCoroutine(StartBattleSequence());
    }

    private IEnumerator StartBattleSequence()
    {
        isTransitioning = true;

        // (선택) 입력 잠금
        if (GameManager.Instance != null)
            GameManager.Instance.isAction = true;

        // --- 복귀 정보 저장 ---
        PlayerReturnContext.ReturnSceneName = SceneManager.GetActiveScene().name;
        PlayerReturnContext.ReturnPosition = (Vector2)returnPoint.position;
        PlayerReturnContext.HasReturnPosition = true;

        // 현재 씬에 있는 SceneFader 찾기
        var fader = FindObjectOfType<SceneFader>(true);
        if (fader != null)
        {
            // FadeOut -> Load
            fader.LoadSceneWithFadeOut(battleSceneName);
        }
        else
        {
            Debug.LogWarning("[BattleTransition] SceneFader를 찾지 못했습니다. 즉시 LoadScene 합니다.");
            SceneManager.LoadScene(battleSceneName);
        }

        yield return null;
    }
}
