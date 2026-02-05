// Assets/Script/Cutscene/Production/CutProductionStepBase.cs
using System.Collections;
using UnityEngine;

public struct CutProductionContext
{
    public string key;
    public GameObject instigator;
    public CutProductionManager manager;
}

public abstract class CutProductionStepBase : MonoBehaviour
{
    [Tooltip("true면 이 Step이 끝날 때까지 Manager가 다음 Step으로 넘어가지 않음")]
    public bool waitForCompletion = true;

    public abstract IEnumerator Execute(CutProductionContext ctx);
}
