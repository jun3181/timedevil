// PlayerData.cs
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    [Header("Identity")]
    public string playerName = "Player";

    [Header("Stats")]
    public int maxHP = 20;
    public int currentHP = 20;
    public int attack = 10;
    public int defense = 5;
    public int speed = 5;     // 선턴 결정 등에 사용

    [Header("Emotion Counter")]
    public int emotionPositive = 0;   // 긍정
    public int emotionNegative = 0;   // 부정

    public bool IsDead => currentHP <= 0;

    /// <summary>초기값 세팅(편의용)</summary>
    public void InitDefaults(
        string name = "Player", int hp = 100, int atk = 10, int def = 5, int spd = 5)
    {
        playerName = string.IsNullOrEmpty(name) ? "Player" : name;
        maxHP = Mathf.Max(1, hp);
        currentHP = maxHP;
        attack = Mathf.Max(0, atk);
        defense = Mathf.Max(0, def);
        speed = Mathf.Max(0, spd);
        emotionPositive = 0;
        emotionNegative = 0;
    }
}
