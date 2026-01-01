// 기존 파일 교체: Assets/Script/loader/ReturnSpawnApplier.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnSpawnApplier : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [Header("옵션")]
    [SerializeField] private bool restoreMonster = true;

    private void Start()
    {
        if (!PlayerReturnContext.HasReturnPosition) return;

        // 같은 씬으로 돌아온 경우에만 적용
        if (PlayerReturnContext.ReturnSceneName == SceneManager.GetActiveScene().name)
        {
            if (playerTransform) playerTransform.position = PlayerReturnContext.ReturnPosition;

            if (restoreMonster && !string.IsNullOrEmpty(PlayerReturnContext.MonsterNameInScene))
            {
                var enemyObj = GameObject.Find(PlayerReturnContext.MonsterNameInScene);
                if (enemyObj) enemyObj.transform.position = PlayerReturnContext.MonsterReturnPosition;
            }
        }
        // 필요 시 한 번 적용 후 초기화
        // PlayerReturnContext.HasReturnPosition = false;
    }
}
