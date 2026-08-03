// EnemySO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Enemy", fileName = "EnemySO")]
public class EnemySO : ScriptableObject
{
    [Header("Identity")]
    public string enemyId = "Enemy1";     // 내부 ID (SelectedEnemyRuntime.enemyName와 매칭)
    public string displayName = "Enemy 1";

    [Header("Base Stats")]
    public int maxHP = 60;
    public int baseATK = 8;
    public int baseDEF = 3;
    public int baseSPD = 6;

    [Header("Optional Deck (future)")]
    public string[] deckIds;
}
