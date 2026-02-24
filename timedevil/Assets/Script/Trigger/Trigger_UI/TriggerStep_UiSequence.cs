// Assets/Script/Trigger/Steps/TriggerStep_UiSequence.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_UiSequence : TriggerStepBase
{
    [Header("Target")]
    [SerializeField] private UiSequencePlayer sequence;

    [Header("Options")]
    [SerializeField] private bool playOnExecute = true;
    [SerializeField] private bool waitUntilFinished = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!sequence)
            sequence = FindObjectOfType<UiSequencePlayer>(true);

        if (!sequence)
        {
            Debug.LogWarning("[TriggerStep_UiSequence] UiSequencePlayer not found.");
            yield break;
        }

        bool done = false;
        System.Action handler = () => done = true;

        if (waitUntilFinished)
            sequence.OnFinished += handler;

        if (playOnExecute)
            sequence.PlayFromStart();

        if (waitUntilFinished)
        {
            while (!done) yield return null;
            sequence.OnFinished -= handler;
        }
    }
}