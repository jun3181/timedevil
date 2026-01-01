using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

/// <summary>
/// 씬이 로드될 때 'PlayerReturnContext'를 확인하여
/// 플레이어를 지정된 복귀 지점으로 이동시키고, '무적 시간'을 부여하는 스크립트.
/// </summary>
public class PlayerReturnManager : MonoBehaviour
{
    [Header("무적 시간 설정")]
    [Tooltip("배틀에서 돌아온 후, 몇 초 동안 배틀 재진입을 막을지 (초 단위)")]
    public float gracePeriodDuration = 3.0f; // (3초가 기본값)

    private void OnEnable()
    {
        // 씬 매니저의 'sceneLoaded' 이벤트에 OnSceneLoaded 함수를 등록(구독)
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[PlayerReturnManager] 씬 로드 이벤트를 '구독'했습니다."); // [로그 1]
    }

    private void OnDisable()
    {
        // 오브젝트가 파괴될 때 등록을 해제(구독 취소) (메모리 누수 방지)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 로드될 때마다 SceneManager가 이 함수를 자동으로 호출합니다.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ▼▼▼ [핵심 디버그] ▼▼▼
        Debug.Log($"[PlayerReturnManager] {scene.name} 씬 로드 완료. 복귀 처리를 시작합니다."); // [로그 2]

        // 1. 'PlayerReturnContext'에 저장된 복귀 데이터가 있는지 확인
        if (PlayerReturnContext.HasReturnPosition)
        {
            Debug.Log("[PlayerReturnManager] 'HasReturnPosition'가 true입니다. 플레이어 이동을 시도합니다."); // [로그 3]
            
            // 2. 씬에 있는 플레이어 오브젝트를 찾음
            PlayerAction player = FindObjectOfType<PlayerAction>();
            if (player != null)
            {
                Debug.Log($"[PlayerReturnManager] 플레이어를 찾았습니다! 이름: {player.name}"); // [로그 4]
                
                // 3. 플레이어의 위치를 저장된 'ReturnPosition'으로 강제 이동
                Vector3 returnPos = PlayerReturnContext.ReturnPosition;
                player.transform.position = returnPos;
                Debug.Log($"[PlayerReturnManager] 플레이어를 {returnPos} 위치로 이동시켰습니다."); // [로그 5]

                // 4. 사용한 복귀 데이터를 리셋
                PlayerReturnContext.HasReturnPosition = false;
                PlayerReturnContext.ReturnSceneName = null;

                // 5. 무적 시간 코루틴 시작
                StartCoroutine(StartGracePeriod());
            }
            else
            {
                // (이 로그가 뜬다면 Script Execution Order 설정을 다시 확인해야 함)
                Debug.LogError("[PlayerReturnManager] 오류: 'HasReturnPosition'는 true지만, 씬에서 PlayerAction을 찾지 못했습니다!"); // [오류 1]
            }
        }
        else
        {
             Debug.LogWarning("[PlayerReturnManager] 'HasReturnPosition'가 false입니다. (정상적인 씬 이동)"); // [경고 1]
        }
    }
    // ▲▲▲ [핵심 디버그 끝] ▲▲▲


    // (무적 시간 코루틴은 동일)
    private IEnumerator StartGracePeriod()
    {
        // 1. 무적 상태 ON (깃발을 올림)
        PlayerReturnContext.IsInGracePeriod = true;
        Debug.Log($"[PlayerReturnManager] 배틀 복귀. {gracePeriodDuration}초간 무적 시간 시작.");

        // 2. 설정된 시간(3초)만큼 대기
        yield return new WaitForSeconds(gracePeriodDuration);

        // 3. 무적 상태 OFF (깃발을 내림)
        PlayerReturnContext.IsInGracePeriod = false;
        Debug.Log("[PlayerReturnManager] 무적 시간 종료.");
    }
}