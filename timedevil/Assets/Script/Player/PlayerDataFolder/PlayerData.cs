// PlayerData.cs
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public const string DefaultPlayerName = "Player";
    public const int DefaultMaxHP = 20;
    public const int DefaultAttack = 10;
    public const int DefaultDefense = 5;
    public const int DefaultSpeed = 5;

    [Header("Identity")]
    public string playerName = DefaultPlayerName;

    [Header("Stats")]
    public int maxHP = DefaultMaxHP;
    public int currentHP = DefaultMaxHP;
    public int attack = DefaultAttack;
    public int defense = DefaultDefense;
    public int speed = DefaultSpeed;     // 선턴 결정 등에 사용

    [Header("Emotion Counter")]
    public int emotionPositive = 0;   // 긍정
    public int emotionNegative = 0;   // 부정

    public bool IsDead => currentHP <= 0;

    /// <summary>초기값 세팅(편의용)</summary>
    public void InitDefaults(
        string name = DefaultPlayerName,
        int hp = DefaultMaxHP,
        int atk = DefaultAttack,
        int def = DefaultDefense,
        int spd = DefaultSpeed)
    {
        playerName = string.IsNullOrEmpty(name) ? DefaultPlayerName : name;
        maxHP = Mathf.Max(1, hp);
        currentHP = maxHP;
        attack = Mathf.Max(0, atk);
        defense = Mathf.Max(0, def);
        speed = Mathf.Max(0, spd);
        emotionPositive = 0;
        emotionNegative = 0;
    }
}
