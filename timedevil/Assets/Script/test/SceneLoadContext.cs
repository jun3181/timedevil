using UnityEngine;

public class SceneLoadContext : MonoBehaviour
{
    public static SceneLoadContext Instance { get; private set; }

    public string pendingEnemyName;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Consume()
    {
        pendingEnemyName = null; // 데이터 사용 후 비워서 재사용 방지
    }
}
