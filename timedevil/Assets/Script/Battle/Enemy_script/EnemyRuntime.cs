// EnemyRuntime.cs
using System;
using UnityEngine;

public class EnemyRuntime : MonoBehaviour
{
    public static EnemyRuntime Instance { get; private set; }

    [Header("Bound SO (read-only source)")]
    [SerializeField] private EnemySO source;

    [Header("Runtime State")]
    public string enemyId;
    public string enemyName;
    public int maxHP;
    public int currentHP;
    public int attack;
    public int defense;
    public int speed;

    public event Action OnChanged; // HP/버프 등 상태 변할 때

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void InitializeFromSO(EnemySO so)
    {
        source = so;
        if (!so)
        {
            Debug.LogError("[EnemyRuntime] EnemySO is null");
            ResetToEmpty();
            return;
        }

        enemyId = so.enemyId;
        enemyName = string.IsNullOrEmpty(so.displayName) ? so.enemyId : so.displayName;
        maxHP = Mathf.Max(1, so.maxHP);
        currentHP = maxHP;
        attack = Mathf.Max(0, so.baseATK);
        defense = Mathf.Max(0, so.baseDEF);
        speed = Mathf.Max(0, so.baseSPD);

        OnChanged?.Invoke();
        Debug.Log($"[EnemyRuntime] Initialized: {enemyName} HP {currentHP}/{maxHP}, ATK {attack}, DEF {defense}, SPD {speed}");
    }

    public void ResetToEmpty()
    {
        enemyId = enemyName = "";
        maxHP = currentHP = attack = defense = speed = 0;
        OnChanged?.Invoke();
    }

    // --- 간단 유틸 ---

    public void TakeDamage(int raw)
    {
        int dmg = Mathf.Max(1, raw - defense);
        currentHP = Mathf.Clamp(currentHP - dmg, 0, maxHP);
        OnChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + Mathf.Max(0, amount), 0, maxHP);
        OnChanged?.Invoke();
    }

    public bool IsDead => currentHP <= 0;
}
