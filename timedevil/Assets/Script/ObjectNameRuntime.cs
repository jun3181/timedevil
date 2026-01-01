using UnityEngine;

/// 피드백: "ObjectNameRuntime : 싱글톤으로 보내기"
/// 배틀 씬에 "어떤 몬스터의 ID를 로드할지" 전달하는 싱글톤
public class ObjectNameRuntime : MonoBehaviour
{
    public static ObjectNameRuntime Instance { get; private set; }

    /// 배틀 씬이 로드된 후, 이 ID를 읽어서
    /// 피드백에서 언급된 "Enemy1" 같은 SO 데이터를 불러옴.
    public string EnemyIDToLoad { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// 배틀 씬으로 전환하기 '직전'에 이 함수를 호출하여 
    /// 몬스터의 ID (예: "Enemy1")를 저장.
    public void SetEnemyToLoad(string enemyID)
    {
        EnemyIDToLoad = enemyID;
        Debug.Log($"[ObjectNameRuntime] 배틀 씬에서 로드할 적 ID 저장: {EnemyIDToLoad}");
    }
}