using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class TriggerStep_NPCAscendFogClear : TriggerStepBase
{
    [Header("NPC")]
    [SerializeField] private Transform npcTarget;
    [Min(0f)] [SerializeField] private float riseDistance = 6f;
    [Min(0f)] [SerializeField] private float spinTurns = 3f;

    [Header("Fog")]
    [SerializeField] private Tilemap fogTilemap;
    [SerializeField] private GameObject fogObject;

    [Header("Timing")]
    [Min(0.01f)] [SerializeField] private float duration = 2.5f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Finish")]
    [SerializeField] private bool disableNpcAtEnd = true;
    [SerializeField] private bool disableFogAtEnd = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (npcTarget == null)
            yield break;

        Vector3 startPosition = npcTarget.position;
        Quaternion startRotation = npcTarget.localRotation;
        Color startFogColor = fogTilemap != null ? fogTilemap.color : Color.white;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float ratio = Mathf.Clamp01(elapsed / duration);
            float eased = ease != null ? ease.Evaluate(ratio) : ratio;

            npcTarget.position = startPosition + Vector3.up * (riseDistance * eased);
            npcTarget.localRotation = startRotation * Quaternion.Euler(0f, 0f, 360f * spinTurns * eased);

            if (fogTilemap != null)
            {
                Color color = startFogColor;
                color.a = Mathf.Lerp(startFogColor.a, 0f, eased);
                fogTilemap.color = color;
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        npcTarget.position = startPosition + Vector3.up * riseDistance;
        npcTarget.localRotation = startRotation * Quaternion.Euler(0f, 0f, 360f * spinTurns);

        if (fogTilemap != null)
        {
            Color color = startFogColor;
            color.a = 0f;
            fogTilemap.color = color;
        }

        if (disableFogAtEnd && fogObject != null)
            fogObject.SetActive(false);

        if (disableNpcAtEnd)
            npcTarget.gameObject.SetActive(false);
    }
}
