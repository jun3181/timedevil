using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Attack Card", fileName = "AttackCard")]
public class AttackCardSO : BaseCardSO
{
    [Header("Attack")]
    public int power = 1;

    // ▼▼ 레거시(웨이브 없을 때만 사용) ▼▼
    [Tooltip("16자리 패턴: 위→아래, 각 행 4칸. 예) 1000 0100 0010 0001 → '1000010000100001'")]
    public string pattern16 = "0000000000000000";

    [Tooltip("타임라인(16개, 초 단위). 비우면 0으로 간주")]
    public float[] timeline = new float[16];

    [Header("Global FX (optional)")]
    public bool cameraShake = false;
    public string animationKey;
    public AudioClip sfx;

    // =========================
    //         Waves Only
    // =========================
    [Serializable]
    public class Wave
    {
        // ───────── Pattern / Timeline (warning layer) ─────────
        [Header("Pattern")]
        [Tooltip("이 웨이브의 16자리 패턴 (경고 마스크)")]
        public string pattern16 = "0000000000000000";

        [Tooltip("이 웨이브의 타임라인(16칸). 길이가 0이면 모두 0초로 간주 (경고 연출용)")]
        public float[] timeline = new float[16];

        // ───────── Wave Timing ─────────
        [Header("Timing")]
        [Tooltip("웨이브 시작 전 지연(초)")]
        public float delayBefore = 0f;

        [Tooltip("웨이브 종료 후 다음 웨이브까지 지연(초)")]
        public float delayAfter = 0f;

        // ───────── Common FX Hooks (optional) ─────────
        [Header("FX Hooks (optional / 공통 훅)")]
        public AudioClip sfx;            // 웨이브용 SFX
        [Tooltip("true: 각 타점마다 / false: 웨이브 시작 1회")]
        public bool sfxEveryHit = true;

        [Tooltip("히트 지점 공통 VFX (폭발/임팩트 등)")]
        public GameObject vfxPrefab;
        [Tooltip("true: 각 타점마다 / false: 웨이브 시작 1회")]
        public bool vfxEveryHit = true;
        [Tooltip("공통 VFX 자동 제거 시간(초)")]
        public float vfxLifetime = 0.6f;

        // ───────── Hit Timing (after warning) ─────────
        [Header("Hit Timing")]
        [Tooltip("각 칸 실제 판정 지연(초). 비우면 0초로 간주 (= 경고 후 히트 대기)")]
        public float[] hitDelays = new float[16];

        // ───────── Launcher (Projectile Hook) ─────────
        [Header("Launcher (Projectile Hook)")]
        [Tooltip("발사체 프리팹(비우면 런처 미사용)")]
        public GameObject projectilePrefab;

        [Tooltip("발사체 속도 (유닛/초). 0이면 즉시 도착 처리")]
        public float projectileSpeed = 8f;

        [Tooltip("충돌 판정 직사각형 가로/세로(발사체 중심 기준, 월드 유닛)")]
        public float projectileHitWidth = 0.8f;
        public float projectileHitHeight = 0.8f;

        [Tooltip("충돌 즉시 파괴")]
        public bool destroyOnImpact = true;

        [Tooltip("발사체 프리팹의 배율(1=원본 크기)")]
        public float projectileScale = 1f;

        [Header("Launcher Label Map (4x4 → 16칸, 0은 비사용)")]
        [Tooltip("출발 라벨(A면=공격자 패널)")]
        public int[] labelsA = new int[16];

        [Tooltip("도착 라벨(B면=상대 패널)")]
        public int[] labelsB = new int[16];

        // ───────── Explosion (Explosion Hook) ─────────
        [Header("Explosion (Explosion Hook)")]
        [Tooltip("폭발 프리팹(비우면 폭발 훅 미사용). 경고 종료 직후 각 칸에 바로 생성됨")]
        public GameObject explosionPrefab;

        [Tooltip("폭발 프리팹 자동 제거 시간(초)")]
        public float explosionLifetime = 0.8f;

        [Tooltip("폭발 프리팹의 배율(1=원본 크기)")]
        public float explosionScale = 1f;

        // ───────── Optional clip selector ─────────
        [Header("Optional: 클립 선택 키")]
        public string clipKey = "";
    }

    [Tooltip("앞에서부터 순차 재생될 웨이브")]
    public Wave[] waves;

    // =========================
    //     런타임 유틸
    // =========================
    public static void ParsePattern16(string pattern, bool[] outMask16)
    {
        if (outMask16 == null || outMask16.Length != 16) return;
        for (int i = 0; i < 16; i++)
        {
            char ch = (pattern != null && pattern.Length > i) ? pattern[i] : '0';
            outMask16[i] = (ch == '1');
        }
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
        if (waves != null)
        {
            foreach (var w in waves)
            {
                if (w == null) continue;
                if (w.delayBefore < 0f) w.delayBefore = 0f;
                if (w.delayAfter < 0f) w.delayAfter = 0f;
                if (w.vfxLifetime < 0f) w.vfxLifetime = 0f;

                if (w.projectileSpeed < 0f) w.projectileSpeed = 0f;
                if (w.projectileHitWidth < 0f) w.projectileHitWidth = 0f;
                if (w.projectileHitHeight < 0f) w.projectileHitHeight = 0f;
                if (w.labelsA == null || w.labelsA.Length != 16) w.labelsA = new int[16];
                if (w.labelsB == null || w.labelsB.Length != 16) w.labelsB = new int[16];

                if (w.explosionLifetime < 0f) w.explosionLifetime = 0f;
            }
        }
    }
#endif
}
