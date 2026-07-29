// Assets/Script/Trigger/TriggerStepBase.cs
using System.Collections;
using UnityEngine;

public abstract class TriggerStepBase : MonoBehaviour, ITriggerStep
{
    public virtual bool AllowPlayerInputWhileExecuting => false;
    public abstract IEnumerator Execute(TriggerContext ctx);
}
