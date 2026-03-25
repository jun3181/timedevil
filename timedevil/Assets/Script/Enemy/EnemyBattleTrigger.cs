using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyBattleTrigger : MonoBehaviour
{
    [Header("Enemy ID (EnemySO.enemyId)")]
    public string enemyID_ToLoad = "Enemy1_Undead";

    [Header("Battle Scene")]
    public string battleSceneName = "battle";

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning || PlayerReturnContext.IsInGracePeriod) return;

        var player = other.GetComponent<PlayerAction>();
        if (!player) return;

        isTransitioning = true;

        if (GameManager.Instance != null) GameManager.Instance.isAction = true;

        var mover = GetComponent<MonsterMover>();
        if (mover) mover.StopChase();

        BattleSceneLoader.Go(battleSceneName, enemyID_ToLoad, player.transform, transform);
    }
}
