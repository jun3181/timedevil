// Assets/Script/Cutscene/Production/Steps/CutStep_Dialogue.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CutStep_Dialogue : CutProductionStepBase
{
    public Dialogue dialogue;

    [Tooltip("대사 시작 시 이미 대사가 켜져있으면 무시")]
    public bool ignoreIfDialogueActive = false;

    public override IEnumerator Execute(CutProductionContext ctx)
    {
        if (dialogue == null)
            yield break;

        var dm = DialogueManager.instance;
        if (dm == null)
            yield break;

        if (ignoreIfDialogueActive && dm.isDialogueActive)
            yield break;

        dm.StartDialogue(dialogue);

        if (waitForCompletion)
        {
            while (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
                yield return null;
        }
    }
}
