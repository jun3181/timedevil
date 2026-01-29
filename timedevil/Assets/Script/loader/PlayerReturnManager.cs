using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerReturnManager : MonoBehaviour
{
    [Header("Grace (재진입 방지)")]
    [SerializeField] private float defaultGraceSeconds = 0f;
    [SerializeField] private bool useUnscaledTimeForGrace = true;

    [Header("Apply Target")]
    [SerializeField] private bool findPlayerMainManager = true;

    [Header("B Suppression (Overlap)")]
    [Tooltip("Overlap 탐색 시 사용할 LayerMask. 모르면 Everything 그대로 두면 됨.")]
    [SerializeField] private LayerMask overlapMask = ~0;

    [Tooltip("TriggerGet만 끌지, Collider도 같이 끌지 선택(안전하게는 둘 다 끄는게 재빨려들기 방지에 강함)")]
    [SerializeField] private bool disableColliderAlso = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (debugLog) Debug.Log("[PlayerReturnManager] subscribed sceneLoaded", this);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // "복귀 씬"에서만 적용
        bool isReturnScene =
            PlayerReturnContext.HasReturnPosition &&
            !string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName) &&
            scene.name == PlayerReturnContext.ReturnSceneName;

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerReturnManager] sceneLoaded => scene='{scene.name}', mode={mode}\n" +
                $"ReturnSceneName='{PlayerReturnContext.ReturnSceneName}', HasReturnPosition={PlayerReturnContext.HasReturnPosition}, ReturnPosition={PlayerReturnContext.ReturnPosition}\n" +
                $"UseOverlapSuppression={PlayerReturnContext.UseOverlapSuppression}, radius={PlayerReturnContext.OverlapRadiusPending}, sec={PlayerReturnContext.OverlapSecondsPending}\n" +
                $"GracePending={PlayerReturnContext.GraceSecondsPending}, IsInGracePeriod={PlayerReturnContext.IsInGracePeriod}",
                this
            );
        }

        if (!isReturnScene)
        {
            if (debugLog)
                Debug.Log("[PlayerReturnManager] skip apply (not a return scene)", this);
            return;
        }

        // 씬 로드 프레임에 Player가 아직 안 잡힐 수 있어서 코루틴으로 처리
        StartCoroutine(CoApplyReturn());
    }

    private IEnumerator CoApplyReturn()
    {
        // 1) Player 찾기(최대 몇 프레임 대기)
        PlayerMainManager player = null;

        if (findPlayerMainManager)
        {
            const int maxWaitFrames = 30;
            for (int i = 0; i < maxWaitFrames; i++)
            {
                player = FindObjectOfType<PlayerMainManager>(true);
                if (player != null) break;
                yield return null;
            }

            if (player == null)
            {
                Debug.LogError("[PlayerReturnManager] PlayerMainManager not found in return scene!", this);
                // 실패해도 컨텍스트는 정리(무한 재시도 방지)
                PlayerReturnContext.ClearReturnCore();
                yield break;
            }
        }

        // 2) 플레이어 이동
        var p = PlayerReturnContext.ReturnPosition;
        if (player != null)
        {
            var tr = player.transform;
            tr.position = new Vector3(p.x, p.y, tr.position.z);

            if (debugLog)
                Debug.Log($"[PlayerReturnManager] moved player => ({p.x:F2},{p.y:F2},{tr.position.z:F2})", this);
        }

        // 3) B Suppression: 복귀 지점 주변 트리거 자동 억제
        if (PlayerReturnContext.UseOverlapSuppression)
        {
            float radius = PlayerReturnContext.OverlapRadiusPending;
            float sec = PlayerReturnContext.OverlapSecondsPending;

            if (radius > 0f && sec > 0f)
            {
                SuppressNearbyTriggers((Vector2)p, radius, sec);
                if (debugLog) Debug.Log($"[PlayerReturnManager] suppressed NEAR return point radius={radius:F2}, sec={sec:F2}", this);
            }
        }

        // 4) Grace
        float grace = PlayerReturnContext.GraceSecondsPending > 0f ? PlayerReturnContext.GraceSecondsPending : defaultGraceSeconds;
        if (grace > 0f) StartCoroutine(CoGrace(grace));
        else
        {
            PlayerReturnContext.IsInGracePeriod = false;
            PlayerReturnContext.GraceSecondsPending = 0f;
        }

        // 5) 1회성 return core 정리
        PlayerReturnContext.ClearReturnCore();
    }

    private void SuppressNearbyTriggers(Vector2 center, float radius, float seconds)
    {
        var cols = Physics2D.OverlapCircleAll(center, radius, overlapMask);
        if (cols == null || cols.Length == 0) return;

        // 비활성/활성 섞여있을 수 있으니, 실제로 끌 대상만 수집해서 seconds 후 복구
        var targets = new List<(TriggerGet tg, Collider2D col)>();

        foreach (var c in cols)
        {
            if (c == null) continue;

            // TriggerGet(너 프로젝트의 트리거 스크립트) 찾기
            var tg = c.GetComponent<TriggerGet>();
            if (tg == null) tg = c.GetComponentInParent<TriggerGet>();

            if (tg != null)
            {
                Collider2D toDisableCol = disableColliderAlso ? c : null;

                // 이미 꺼져있으면 스킵(중복 억제 방지)
                if (!tg.enabled && (toDisableCol == null || !toDisableCol.enabled))
                    continue;

                // 끄기
                tg.enabled = false;
                if (toDisableCol != null) toDisableCol.enabled = false;

                targets.Add((tg, toDisableCol));
            }
        }

        if (targets.Count > 0)
            StartCoroutine(CoRestoreSuppressed(targets, seconds));
    }

    private IEnumerator CoRestoreSuppressed(List<(TriggerGet tg, Collider2D col)> targets, float seconds)
    {
        if (useUnscaledTimeForGrace) yield return new WaitForSecondsRealtime(seconds);
        else yield return new WaitForSeconds(seconds);

        foreach (var t in targets)
        {
            if (t.tg != null) t.tg.enabled = true;
            if (t.col != null) t.col.enabled = true;
        }

        if (debugLog) Debug.Log("[PlayerReturnManager] restore suppressed triggers", this);
    }

    private IEnumerator CoGrace(float seconds)
    {
        PlayerReturnContext.IsInGracePeriod = true;
        PlayerReturnContext.GraceSecondsPending = 0f;

        if (debugLog) Debug.Log($"[PlayerReturnManager] grace start {seconds:F2}s", this);

        if (useUnscaledTimeForGrace)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);

        PlayerReturnContext.IsInGracePeriod = false;
        if (debugLog) Debug.Log("[PlayerReturnManager] grace end", this);
    }
}
