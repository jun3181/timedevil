using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionScript : ItemScriptBase
{
    public override void Run() {
        Debug.Log("루시의 스탯");
        if(PlayerDataRuntime.Instance) {
            Debug.Log($"currentHP: {PlayerDataRuntime.Instance.Data.currentHP}");
        } else {
            Debug.Log("와피스");
        }
    }
}
