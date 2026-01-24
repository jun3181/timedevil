// Assets/Script/Trigger/ITriggerStep.cs
using System.Collections;

public interface ITriggerStep
{
    IEnumerator Execute(TriggerContext ctx);
}
