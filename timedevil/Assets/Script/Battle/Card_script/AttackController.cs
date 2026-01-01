using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public enum TimelineMode { StartTimes, Windows }

    private const float ProjectileZ = -5f;   // 발사체는 항상 앞(카메라 쪽)
    private const float ExplosionZ = -5f;    // 폭발도 앞

    [SerializeField] private AttackAnimationController anim;

    [Header("Manual Centers (World Positions)")]
    [SerializeField] private Vector3[] playerCentersPos = new Vector3[16];
    [SerializeField] private Vector3[] enemyCentersPos = new Vector3[16];

    [Header("Hit AABB (for warning-only direct hit)")]
    [SerializeField] private float tileWidth = 1.3f;
    [SerializeField] private float tileHeight = 1.3f;
    [SerializeField] private float tileZ = 0f;

    [Header("Timeline Interpretation")]
    [SerializeField] private TimelineMode timelineMode = TimelineMode.Windows;
    [SerializeField] private float defaultTailWindow = 0.20f;
    [SerializeField] private float minWindow = 0.06f;

    [Header("Hit/VFX")]
    [SerializeField] private HPController hp;
    [SerializeField] private bool oneHitPerCard = true;

    void Awake()
    {
        if (!hp) hp = FindObjectOfType<HPController>(true);
        if (!anim) anim = FindObjectOfType<AttackAnimationController>(true);
    }

    public IEnumerator Execute(AttackCardSO so, Faction self, Faction foe)
    {
        if (!so) yield break;
        if (!anim) { Debug.LogWarning("[AttackController] AttackAnimationController missing."); yield break; }

        hp?.BeginCardHitTest(foe);

        var centersSelf = (self == Faction.Player) ? playerCentersPos : enemyCentersPos;
        var centersFoe = (foe == Faction.Player) ? playerCentersPos : enemyCentersPos;
        if (centersSelf == null || centersSelf.Length < 16 || centersFoe == null || centersFoe.Length < 16)
        { Debug.LogWarning("[AttackController] centers arrays must have 16 elements."); yield break; }

        anim.EnsurePool(16);

        // ─── 웨이브가 없으면: 레거시 ───
        if (so.waves == null || so.waves.Length == 0)
        {
            bool[] mask = new bool[16];
            float[] times = new float[16];
            AttackCardSO.ParsePattern16(so.pattern16, mask);
            AttackCardSO.FillTimeline16(so.timeline, times);

            if (IsAllZero(mask)) yield break;

            anim.PlaceAndShowMask(mask, centersFoe);
            yield return RunWarningTimeline(mask, times);
            anim.HideAll();

            yield return RunDirectHitsByDelays(mask, times, centersFoe, so, self, foe);
            yield break;
        }

        // ─── 웨이브 기반 ───
        for (int wi = 0; wi < so.waves.Length; wi++)
        {
            var w = so.waves[wi];
            if (w == null) continue;

            if (w.delayBefore > 0f) yield return new WaitForSeconds(w.delayBefore);

            // 경고용 기본 마스크/타이밍
            bool[] warnMaskBase = new bool[16];
            float[] warnTimes = new float[16];
            AttackCardSO.ParsePattern16(w.pattern16, warnMaskBase);
            AttackCardSO.FillTimeline16(w.timeline, warnTimes);

            // (1) 경고 마스크 강화: labelsB>0 인 모든 도착 타일 OR
            bool[] warnMask = BuildWarningMaskWithLabelsB(warnMaskBase, w.labelsB);

            if (IsAllZero(warnMask))
            {
                if (w.delayAfter > 0f) yield return new WaitForSeconds(w.delayAfter);
                continue;
            }

            // (경고는 "맞을 면"에 표시)
            anim.PlaceAndShowMask(warnMask, centersFoe);

            // 웨이브 시작 SFX/VFX(옵션)
            if (!w.sfxEveryHit && w.sfx)
                AudioSource.PlayClipAtPoint(w.sfx, AveragePosition(warnMask, centersFoe));
            if (!w.vfxEveryHit && w.vfxPrefab)
                SpawnVfx(w.vfxPrefab, AveragePosition(warnMask, centersFoe), w.vfxLifetime);

            // 경고 타임라인 진행
            yield return RunWarningTimeline(warnMask, warnTimes);
            anim.HideAll();

            // Hook 선택
            if (w.projectilePrefab != null)                       // Launcher Hook (이동 중 충돌 즉시 판정)
            {
                yield return LaunchProjectilesByLabels(w, self, foe, centersSelf, centersFoe, so);
            }
            else if (w.explosionPrefab != null)                   // Explosion Hook
            {
                yield return RunExplosionHook(warnMask, w.hitDelays, centersFoe, w, so, self, foe);
            }
            else                                                  // Hook 없음 → 지연 직히트
            {
                yield return RunDirectHitsByDelays(warnMask, w.hitDelays, centersFoe, so, self, foe);
            }

            if (w.delayAfter > 0f) yield return new WaitForSeconds(w.delayAfter);
        }
    }

    // ─────────────────────────────────────────────
    //  경고(빨간 타일) 타임라인만 수행
    // ─────────────────────────────────────────────
    private IEnumerator RunWarningTimeline(bool[] mask, float[] timeline)
    {
        if (timelineMode == TimelineMode.StartTimes)
        {
            var schedule = BuildSchedule(mask, timeline);
            float lastT = 0f;
            for (int k = 0; k < schedule.Count; k++)
            {
                var (idx, tPoint) = schedule[k];
                float wait = Mathf.Max(0f, tPoint - lastT);
                if (wait > 0f) yield return new WaitForSeconds(wait);
                lastT = tPoint;

                float nextT = (k + 1 < schedule.Count) ? schedule[k + 1].time : tPoint + defaultTailWindow;
                float window = Mathf.Max(minWindow, nextT - tPoint);
                anim.StartPulseWindow(idx, window);
            }
            if (schedule.Count > 0 && defaultTailWindow > 0f)
                yield return new WaitForSeconds(defaultTailWindow);
        }
        else // Windows
        {
            float maxWin = 0f;
            for (int i = 0; i < 16; i++)
            {
                if (mask != null && i < mask.Length && mask[i])
                {
                    float win = (timeline != null && timeline.Length > i) ? timeline[i] : 0f;
                    if (win > 0f)
                    {
                        anim.StartPulseWindow(i, win);
                        if (win > maxWin) maxWin = win;
                    }
                }
            }
            if (maxWin > 0f) yield return new WaitForSeconds(maxWin);
        }
    }

    // ─────────────────────────────────────────────
    //  Hook 없음: 칸별 지연 후 즉시 히트
    // ─────────────────────────────────────────────
    private IEnumerator RunDirectHitsByDelays(
        bool[] mask, float[] delays, Vector3[] centers,
        AttackCardSO so, Faction self, Faction foe)
    {
        bool damageApplied = false;
        var running = new List<Coroutine>(16);

        for (int i = 0; i < 16; i++)
        {
            if (mask != null && i < mask.Length && mask[i])
            {
                float d = (delays != null && delays.Length > i) ? Mathf.Max(0f, delays[i]) : 0f;
                var pos = centers[i]; pos.z = tileZ;

                running.Add(StartCoroutine(CoDelayThenRectHit(
                    d, pos, so, self, foe,
                    () => damageApplied, () => damageApplied = true
                )));
            }
        }
        foreach (var co in running) if (co != null) yield return co;
    }

    private IEnumerator CoDelayThenRectHit(
        float delay, Vector3 center, AttackCardSO so, Faction self, Faction foe,
        Func<bool> getDamageApplied, Action setDamageAppliedTrue)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (getDamageApplied() && oneHitPerCard) yield break;

        if (CheckRectHitNow(center, tileWidth, tileHeight))
        {
            ApplyDamage(so, self, foe);
            setDamageAppliedTrue();
        }
    }

    // ─────────────────────────────────────────────
    //  Explosion Hook: 경고 후 즉시 프리팹 표시(Z=-5), hitDelays 후 판정
    // ─────────────────────────────────────────────
    private IEnumerator RunExplosionHook(
        bool[] mask, float[] hitDelays, Vector3[] centersFoe,
        AttackCardSO.Wave w, AttackCardSO so, Faction self, Faction foe)
    {
        if (w.explosionPrefab == null) yield break;

        // 1) 각 칸에 즉시 프리팹 생성(Z=-5), lifetime 후 제거 + 스케일
        var spawned = new List<GameObject>(16);
        for (int i = 0; i < 16; i++)
        {
            if (mask != null && i < mask.Length && mask[i])
            {
                var p = centersFoe[i]; p.z = ExplosionZ;
                var go = Instantiate(w.explosionPrefab, p, Quaternion.identity);

                if (w.explosionScale > 0f) go.transform.localScale *= w.explosionScale;

                spawned.Add(go);
                if (w.explosionLifetime > 0f) Destroy(go, w.explosionLifetime);
            }
        }

        // 2) 칸별 hitDelay 후 히트 판정
        bool damageApplied = false;
        var running = new List<Coroutine>(16);

        for (int i = 0; i < 16; i++)
        {
            if (mask != null && i < mask.Length && mask[i])
            {
                float d = (hitDelays != null && hitDelays.Length > i) ? Mathf.Max(0f, hitDelays[i]) : 0f;
                var pos = centersFoe[i]; pos.z = tileZ; // 판정용 Z는 무관

                running.Add(StartCoroutine(CoDelayThenRectHit(
                    d, pos, so, self, foe,
                    () => damageApplied, () => damageApplied = true
                )));
            }
        }

        foreach (var co in running) if (co != null) yield return co;
    }

    // ─────────────────────────────────────────────
    //  Launcher: 라벨 매칭으로 발사체 직선 이동
    //  (★★ 수정: 이동 중 충돌 즉시 데미지/소멸 처리)
    // ─────────────────────────────────────────────
    private IEnumerator LaunchProjectilesByLabels(
        AttackCardSO.Wave w, Faction self, Faction foe,
        Vector3[] centersA, Vector3[] centersB, AttackCardSO so)
    {
        var labA = w.labelsA ?? new int[16];
        var labB = w.labelsB ?? new int[16];

        var mapA = GroupLabelIndices(labA);
        var mapB = GroupLabelIndices(labB);

        bool damageApplied = false;
        var running = new List<Coroutine>();

        foreach (var kv in mapA) // label → src indices
        {
            int label = kv.Key; if (label == 0) continue;
            if (!mapB.TryGetValue(label, out var dstList)) continue;

            var srcList = kv.Value;
            for (int si = 0; si < srcList.Count; si++)
            {
                int srcIdx = srcList[si];
                var startPos = centersA[srcIdx]; startPos.z = ProjectileZ; // Z 고정

                for (int di = 0; di < dstList.Count; di++)
                {
                    int dstIdx = dstList[di];
                    var endPos = centersB[dstIdx]; endPos.z = ProjectileZ; // Z 고정

                    float launchDelay = 0f;
                    if (w.hitDelays != null && w.hitDelays.Length > srcIdx)
                        launchDelay = Mathf.Max(0f, w.hitDelays[srcIdx]); // 출발칸 기준 지연

                    running.Add(StartCoroutine(CoLaunchProjectileLine_MoveHit(
                        w, startPos, endPos, launchDelay, so, self, foe,
                        () => damageApplied, () => damageApplied = true
                    )));
                }
            }
        }

        foreach (var co in running) if (co != null) yield return co;
    }

    private Dictionary<int, List<int>> GroupLabelIndices(int[] labels16)
    {
        var dict = new Dictionary<int, List<int>>();
        if (labels16 == null || labels16.Length != 16) return dict;
        for (int i = 0; i < 16; i++)
        {
            int lab = labels16[i];
            if (!dict.TryGetValue(lab, out var list)) { list = new List<int>(); dict[lab] = list; }
            list.Add(i);
        }
        return dict;
    }

    // ★ 새 버전: 이동 중 매 프레임 AABB로 충돌 체크 → 즉시 데미지/임팩트 → (옵션) 파괴
    private IEnumerator CoLaunchProjectileLine_MoveHit(
        AttackCardSO.Wave w,
        Vector3 startPos, Vector3 endPos, float delay,
        AttackCardSO so, Faction self, Faction foe,
        Func<bool> getDamageApplied, Action setDamageAppliedTrue)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        GameObject proj = null;
        if (w.projectilePrefab != null)
        {
            proj = Instantiate(w.projectilePrefab, startPos, Quaternion.identity);
            if (w.projectileScale > 0f) proj.transform.localScale *= w.projectileScale;
        }

        float dist = Vector3.Distance(startPos, endPos);
        float speed = Mathf.Max(0f, w.projectileSpeed);
        float t = (dist <= 0.0001f || speed <= 0f) ? 1f : 0f; // 속도 0 → 즉시 도착

        // 히트박스 최소 보정
        float hbW = Mathf.Max(0.01f, w.projectileHitWidth);
        float hbH = Mathf.Max(0.01f, w.projectileHitHeight);

        while (t < 1f)
        {
            t += (speed * Time.deltaTime) / Mathf.Max(0.0001f, dist);
            t = Mathf.Clamp01(t);

            var pos = Vector3.Lerp(startPos, endPos, t);
            pos.z = ProjectileZ;
            if (proj) proj.transform.position = pos;

            // 이동 중 충돌: 한 카드당 1회만 데미지(옵션)
            if (!(getDamageApplied() && oneHitPerCard))
            {
                if (CheckRectHitNow(pos, hbW, hbH))
                {
                    ApplyDamage(so, self, foe);
                    setDamageAppliedTrue();

                    if (w.vfxPrefab) SpawnVfx(w.vfxPrefab, pos, w.vfxLifetime);
                    if (proj && w.destroyOnImpact) Destroy(proj);
                    yield break; // 이 발사체 종료
                }
            }

            yield return null;
        }

        // 도착했지만 이동 중 한 번도 맞추지 못했으면 소멸
        if (proj) Destroy(proj);
    }

    // ─────────────────────────────────────────────
    //  공통 유틸
    // ─────────────────────────────────────────────
    private bool[] BuildWarningMaskWithLabelsB(bool[] baseMask, int[] labelsB)
    {
        // baseMask OR (labelsB>0)
        var outMask = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            bool fromBase = (baseMask != null && baseMask.Length > i) ? baseMask[i] : false;
            bool fromLabel = (labelsB != null && labelsB.Length > i) ? (labelsB[i] > 0) : false;
            outMask[i] = fromBase || fromLabel;
        }
        return outMask;
    }

    private List<(int idx, float time)> BuildSchedule(bool[] mask, float[] times)
    {
        var list = new List<(int idx, float time)>(16);
        for (int i = 0; i < 16; i++)
        {
            if (mask != null && i < mask.Length && mask[i])
            {
                float t = (times != null && times.Length > i) ? Mathf.Max(0f, times[i]) : 0f;
                list.Add((i, t));
            }
        }
        list.Sort((a, b) => a.time.CompareTo(b.time));
        return list;
    }

    private bool CheckRectHitNow(Vector3 center, float w, float h)
    {
        var foePos = hp ? hp.GetWorldPositionOfPawn(hp.CurrentDamageTarget) : Vector3.positiveInfinity;
        float halfX = w * 0.5f, halfY = h * 0.5f;
        return (foePos.x >= center.x - halfX && foePos.x <= center.x + halfX &&
                foePos.y >= center.y - halfY && foePos.y <= center.y + halfY);
    }

    private void ApplyDamage(AttackCardSO so, Faction self, Faction foe)
    {
        if (hp == null) return;
        int atk = hp.GetAttack(self);
        int def = hp.GetDefense(foe);
        int dmg = Mathf.Max(1, (so.power + atk) - def);
        hp.ApplyDamage(foe, dmg);
    }

    private static bool IsAllZero(bool[] mask)
    {
        if (mask == null || mask.Length == 0) return true;
        for (int i = 0; i < mask.Length; i++) if (mask[i]) return false;
        return true;
    }

    private static Vector3 AveragePosition(bool[] mask, Vector3[] centers)
    {
        Vector3 sum = Vector3.zero; int cnt = 0;
        for (int i = 0; i < 16 && i < centers.Length; i++)
            if (mask != null && i < mask.Length && mask[i]) { var p = centers[i]; p.z = 0f; sum += p; cnt++; }
        if (cnt == 0) return Vector3.zero;
        var avg = sum / cnt; avg.z = 0f; return avg;
    }

    private static void SpawnVfx(GameObject prefab, Vector3 pos, float life)
    {
        if (!prefab) return;
        var go = GameObject.Instantiate(prefab, pos, Quaternion.identity);
        if (life > 0f) GameObject.Destroy(go, life);
    }
}
