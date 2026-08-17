// Assets/Script/BattleTransition.cs
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BattleTransition : MonoBehaviour, IInteractable
{
    [Header("로드할 배틀씬")]
    [Tooltip("빌드 세팅에 등록된 씬 이름")]
    public string battleSceneName = "battle";

    [Header("복귀 지점")]
    [Tooltip("배틀 종료 후 이 씬으로 돌아왔을 때 플레이어가 나타날 위치")]
    public Transform returnPoint;
    [Header("복귀 Grace")]
    public float graceSeconds = 0.5f;

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

        // --- 복귀 정보 저장(공용 API 사용) ---
        bool restoreCam = false;
        CameraModeId camMode = CameraModeId.Fixed;
        float camOrtho = 0f;
        Vector2 camFixed = returnPoint.position;
        string camBounds = null;

        var cm = CameraManager.Instance != null ? CameraManager.Instance : FindObjectOfType<CameraManager>(true);
        if (cm != null && cm.TryGetSnapshot(out camMode, out camOrtho, out Vector3 fixedPos3, out string boundsName))
        {
            restoreCam = true;
            camFixed = new Vector2(fixedPos3.x, fixedPos3.y);
            camBounds = string.IsNullOrWhiteSpace(boundsName) ? null : boundsName;
        }

        PlayerReturnContext.SetReturnFromTrigger(
            returnSceneName: SceneManager.GetActiveScene().name,
            returnPosition: returnPoint.position,
            graceSeconds: Mathf.Max(0f, graceSeconds),
            requestCameraRebind: false,
            targetVcamName: null,
            useOverlapSuppression: false,
            overlapRadius: 0f,
            overlapSeconds: 0f,
            restoreCameraState: restoreCam,
            cameraMode: camMode,
            cameraOrthoSize: camOrtho,
            cameraFixedPos: camFixed,
            cameraBoundsName: camBounds
        );

        SceneTransitionService.EnterBattle(battleSceneName, null, null, null, useFaderIfExists: true);

        yield return null;
    }
}
