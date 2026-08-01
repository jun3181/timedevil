using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class TriggerStep_NPCAscendFogClear : TriggerStepBase
{
    [Header("NPC")]
    [SerializeField] private Transform npcTarget;
    [SerializeField] private SpriteRenderer npcRenderer;
    [Min(0f)] [SerializeField] private float riseDistance = 6f;

    [Header("Spin Sprites")]
    [Tooltip("회전 방향 순서대로 재생할 스프라이트")]
    [SerializeField] private List<Sprite> spinSprites = new();
    [Min(0.01f)] [SerializeField] private float secondsPerSpinSprite = 0.12f;

    private static readonly string[] RequiredSpinSpriteNames =
    {
        "FD_Character_036_1",
        "FD_Character_036_5",
        "FD_Character_036_13",
        "FD_Character_036_9"
    };

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

        ResolveSpinSpritesByName();

        Vector3 startPosition = npcTarget.position;
        Color startFogColor = fogTilemap != null ? fogTilemap.color : Color.white;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float ratio = Mathf.Clamp01(elapsed / duration);
            float eased = ease != null ? ease.Evaluate(ratio) : ratio;

            npcTarget.position = startPosition + Vector3.up * (riseDistance * eased);
            ApplySpinSprite(elapsed);

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

    private void ApplySpinSprite(float elapsed)
    {
        if (npcRenderer == null || spinSprites == null || spinSprites.Count == 0)
            return;

        int index = Mathf.FloorToInt(elapsed / secondsPerSpinSprite) % spinSprites.Count;
        Sprite sprite = spinSprites[index];
        if (sprite != null)
            npcRenderer.sprite = sprite;
    }

    private void ResolveSpinSpritesByName()
    {
        if (npcRenderer == null && npcTarget != null)
            npcRenderer = npcTarget.GetComponent<SpriteRenderer>();

        if (HasAllSpinSprites())
            return;

#if UNITY_EDITOR
        if (TryResolveSpinSpritesFromAssetDatabase())
            return;
#endif

        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        List<Sprite> resolved = new(RequiredSpinSpriteNames.Length);

        for (int i = 0; i < RequiredSpinSpriteNames.Length; i++)
        {
            Sprite match = null;
            for (int j = 0; j < loadedSprites.Length; j++)
            {
                if (loadedSprites[j] != null && loadedSprites[j].name == RequiredSpinSpriteNames[i])
                {
                    match = loadedSprites[j];
                    break;
                }
            }

            if (match != null)
                resolved.Add(match);
        }

        if (resolved.Count == RequiredSpinSpriteNames.Length)
            spinSprites = resolved;
        else
            Debug.LogWarning("[TriggerStep_NPCAscendFogClear] FD_Character_036 회전 스프라이트 4개를 모두 찾지 못했습니다.", this);
    }

#if UNITY_EDITOR
    private bool TryResolveSpinSpritesFromAssetDatabase()
    {
        List<Sprite> resolved = new(RequiredSpinSpriteNames.Length);

        for (int i = 0; i < RequiredSpinSpriteNames.Length; i++)
        {
            string requiredName = RequiredSpinSpriteNames[i];
            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets($"{requiredName} t:Sprite");
            Sprite match = null;

            for (int j = 0; j < assetGuids.Length && match == null; j++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[j]);
                Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                for (int k = 0; k < assets.Length; k++)
                {
                    if (assets[k] is Sprite sprite && sprite.name == requiredName)
                    {
                        match = sprite;
                        break;
                    }
                }
            }

            if (match == null)
                return false;

            resolved.Add(match);
        }

        spinSprites = resolved;
        UnityEditor.EditorUtility.SetDirty(this);
        return true;
    }
#endif

    private bool HasAllSpinSprites()
    {
        if (spinSprites == null || spinSprites.Count != RequiredSpinSpriteNames.Length)
            return false;

        for (int i = 0; i < spinSprites.Count; i++)
        {
            if (spinSprites[i] == null || spinSprites[i].name != RequiredSpinSpriteNames[i])
                return false;
        }

        return true;
    }
}
