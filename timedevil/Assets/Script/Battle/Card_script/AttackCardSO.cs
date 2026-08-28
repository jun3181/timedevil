using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttackCastType
{
    Instant,
    Projectile
}

[Serializable]
public class AttackGridMask
{
    [SerializeField] private bool[] cells = new bool[16];

    public bool IsEmpty()
    {
        if (cells == null) return true;
        for (int i = 0; i < cells.Length; i++)
            if (cells[i]) return false;
        return true;
    }

    public void CopyTo(bool[] outMask16)
    {
        if (outMask16 == null || outMask16.Length < 16) return;

        EnsureSize();
        for (int i = 0; i < 16; i++)
            outMask16[i] = cells[i];
    }

    public void EnsureSize()
    {
        if (cells != null && cells.Length == 16)
            return;

        bool[] resized = new bool[16];
        if (cells != null)
        {
            int copyCount = Mathf.Min(cells.Length, resized.Length);
            for (int i = 0; i < copyCount; i++)
                resized[i] = cells[i];
        }

        cells = resized;
    }
}

[Serializable]
public class AttackGridCell
{
    [SerializeField, Range(0, 15)] private int index;

    public int Index
    {
        get { return Mathf.Clamp(index, 0, 15); }
    }

    public void EnsureValid()
    {
        index = Mathf.Clamp(index, 0, 15);
    }
}

[Serializable]
public class AttackProjectileRoute
{
    public AttackGridCell from = new AttackGridCell();
    public AttackGridCell to = new AttackGridCell();
    [Min(0f)] public float launchDelay = 0f;

    public int FromIndex
    {
        get { return from != null ? from.Index : 0; }
    }

    public int ToIndex
    {
        get { return to != null ? to.Index : 0; }
    }

    public void EnsureValid()
    {
        if (from == null) from = new AttackGridCell();
        if (to == null) to = new AttackGridCell();
        from.EnsureValid();
        to.EnsureValid();
        if (launchDelay < 0f) launchDelay = 0f;
    }
}

[CreateAssetMenu(menuName = "Cards/Attack Card", fileName = "AttackCard")]
public class AttackCardSO : BaseCardSO
{
    [Header("Attack")]
    public int power = 1;

    [HideInInspector] public AttackGridMask hitMask = new AttackGridMask();
    [HideInInspector] public float[] timeline = new float[16];

    [Header("Global FX (optional)")]
    public bool cameraShake = false;
    public string animationKey;
    public AudioClip sfx;

    [Serializable]
    public class Wave
    {
        public AttackCastType castType = AttackCastType.Instant;

        public AttackGridMask hitMask = new AttackGridMask();

        public float[] timeline = new float[16];

        [Min(0f)] public float delayBefore = 0f;
        [Min(0f)] public float delayAfter = 0f;

        public AudioClip sfx;
        public bool sfxEveryHit = true;

        public GameObject vfxPrefab;
        public bool vfxEveryHit = true;
        [Min(0f)] public float vfxLifetime = 0.6f;

        public float[] hitDelays = new float[16];

        public List<AttackProjectileRoute> projectileRoutes = new List<AttackProjectileRoute>
        {
            new AttackProjectileRoute()
        };

        public GameObject projectilePrefab;

        [Min(0f)] public float projectileSpeed = 8f;

        [Min(0.01f)] public float projectileHitWidth = 0.8f;
        [Min(0.01f)] public float projectileHitHeight = 0.8f;

        public bool destroyOnImpact = true;

        [Min(0f)] public float projectileScale = 1f;

        [HideInInspector] public int[] labelsA = new int[16];
        [HideInInspector] public int[] labelsB = new int[16];

        public GameObject explosionPrefab;

        [Min(0f)] public float explosionLifetime = 0.8f;

        [Min(0f)] public float explosionScale = 1f;

        public string clipKey = "";

        public bool HasProjectileRoutes()
        {
            return projectileRoutes != null && projectileRoutes.Count > 0;
        }
    }

    public Wave[] waves = { new Wave() };

    public static void FillMask16(AttackGridMask mask, bool[] outMask16)
    {
        if (outMask16 == null || outMask16.Length != 16) return;

        for (int i = 0; i < 16; i++)
            outMask16[i] = false;

        if (mask != null && !mask.IsEmpty())
            mask.CopyTo(outMask16);
    }

    public static void FillTimeline16(float[] src, float[] outTimes16)
    {
        if (outTimes16 == null || outTimes16.Length != 16) return;
        for (int i = 0; i < 16; i++)
        {
            float t = (src != null && src.Length > i) ? src[i] : 0f;
            outTimes16[i] = t;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        type = CardType.Attack;

        if (hitMask == null) hitMask = new AttackGridMask();
        hitMask.EnsureSize();

        timeline = EnsureFloatArray16(timeline);

        if (waves == null) return;
        foreach (Wave wave in waves)
        {
            if (wave == null) continue;

            if (wave.hitMask == null) wave.hitMask = new AttackGridMask();
            wave.hitMask.EnsureSize();

            wave.timeline = EnsureFloatArray16(wave.timeline);
            wave.hitDelays = EnsureFloatArray16(wave.hitDelays);
            wave.labelsA = EnsureIntArray16(wave.labelsA);
            wave.labelsB = EnsureIntArray16(wave.labelsB);

            if (wave.projectileRoutes == null)
                wave.projectileRoutes = new List<AttackProjectileRoute>();

            foreach (AttackProjectileRoute route in wave.projectileRoutes)
            {
                if (route != null) route.EnsureValid();
            }

            if (wave.delayBefore < 0f) wave.delayBefore = 0f;
            if (wave.delayAfter < 0f) wave.delayAfter = 0f;
            if (wave.vfxLifetime < 0f) wave.vfxLifetime = 0f;
            if (wave.projectileSpeed < 0f) wave.projectileSpeed = 0f;
            if (wave.projectileHitWidth < 0.01f) wave.projectileHitWidth = 0.01f;
            if (wave.projectileHitHeight < 0.01f) wave.projectileHitHeight = 0.01f;
            if (wave.projectileScale < 0f) wave.projectileScale = 0f;
            if (wave.explosionLifetime < 0f) wave.explosionLifetime = 0f;
            if (wave.explosionScale < 0f) wave.explosionScale = 0f;
        }
    }

    private static float[] EnsureFloatArray16(float[] src)
    {
        if (src != null && src.Length == 16) return src;

        float[] resized = new float[16];
        if (src != null)
        {
            int copyCount = Mathf.Min(src.Length, resized.Length);
            for (int i = 0; i < copyCount; i++)
                resized[i] = src[i];
        }

        return resized;
    }

    private static int[] EnsureIntArray16(int[] src)
    {
        if (src != null && src.Length == 16) return src;

        int[] resized = new int[16];
        if (src != null)
        {
            int copyCount = Mathf.Min(src.Length, resized.Length);
            for (int i = 0; i < copyCount; i++)
                resized[i] = src[i];
        }

        return resized;
    }
#endif
}
