using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionItemScript : ItemScriptBase
{
    [Header("최대 체력 변화량")]
    [SerializeField]
    private int deltaMaxHP = 0;

    [Header("체력 변화량")]
    [SerializeField]
    private int deltaCurrentHP = 0;

    [Header("공격력 변화량")]
    [SerializeField]
    private int deltaAttack = 0;

    [Header("방어력 변화량")]
    [SerializeField]
    private int deltaDefense = 0;

    [Header("속력 변화량")]
    [SerializeField]
    private int deltaSpeed = 0;

    [Header("디버그 메시지 출력")]
    [SerializeField]
    private bool debuged = false;

    public override void Run() {
        PlayerData player = PlayerDataRuntime.Instance.Data;
            
        player.maxHP += deltaMaxHP;
        if(player.maxHP < 0) player.maxHP = 0;

        player.currentHP += deltaCurrentHP;
        if(player.currentHP>player.maxHP) player.currentHP = player.maxHP;

        player.attack += deltaAttack;
        if(player.attack < 0) player.attack = 0;

        player.defense += deltaDefense;
        if(player.defense < 0) player.defense = 0;
        
        player.speed += deltaSpeed;
        if(player.speed < 1) player.speed = 1;

        if(debuged) {
            Debug.Log($"최대 체력: {player.maxHP}, 현제 체력: {player.currentHP}, 공격력: {player.attack}, 방어력: {player.defense}, 속력: {player.speed}");
        }
    }

    public override bool CanItemUsed(out string msg) {
        bool flag = PlayerDataRuntime.Instance != null;

        msg = "";
        if(!flag) {
            msg = "PlayerDataRuntime의 인스턴스가 존재하지 않습니다.";
            if(debuged) Debug.LogWarning(msg);
        }

        return flag;
    }
}
