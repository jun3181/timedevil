using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class MoveTutorialBattleOverride : MonoBehaviour
{
    [SerializeField] private string enemyId = "EnemyTutorial";

    void Awake()
    {
        if (!string.IsNullOrWhiteSpace(enemyId))
            BattleSceneLoader.enemyIdToLoad = enemyId;
    }
}
