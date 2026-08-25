using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Enemy", fileName = "EnemySO")]
public class EnemySO : ScriptableObject
{
    [Header("Identity")]
    public string enemyId = "Enemy1";     // Matches SelectedEnemyRuntime.enemyName.
    public string displayName = "Enemy 1";

    [Header("Visuals")]
    [Tooltip("Sprite used by Enemy(tem), the larger state-view enemy.")]
    public Sprite stateSprite;
    [Tooltip("Sprite used by none, the gameplay enemy.")]
    public Sprite gameplaySprite;

    [Header("Base Stats")]
    public int maxHP = 60;
    public int baseATK = 8;
    public int baseDEF = 3;
    public int baseSPD = 6;

    [Header("Optional Deck (future)")]
    public string[] deckIds;
}
