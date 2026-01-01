using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyBattleTrigger : MonoBehaviour
{
    [Header("몬스터 ID (EnemySO.enemyId)")]
    public string enemyID_ToLoad = "Enemy1_Undead";

    [Header("배틀 씬 이름")]
    public string battleSceneName = "battle";   // 네 프로젝트의 배틀씬 이름으로

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning || PlayerReturnContext.IsInGracePeriod) return;

        var player = other.GetComponent<PlayerAction>();
        if (!player) return;

        isTransitioning = true;

        // (선택) 플레이어 조작 잠금
        if (GameManager.Instance != null) GameManager.Instance.isAction = true;

        // (선택) NPC 이동 멈춤
        var mover = GetComponent<UndeadMover>();
        if (mover) mover.StopAllCoroutines();

        // 핵심: 이름만 넘기고, 나머지는 로더에 위임
        BattleSceneLoader.Go(battleSceneName, enemyID_ToLoad, player.transform, transform);
    }
}
