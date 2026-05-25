// Assets/Script/loader/PlayerReturnManager.cs
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

    [Tooltip("TriggerGet만 끌지, Collider도 같이 끌지 선택")]
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
        bool isReturnScene =
            PlayerReturnContext.HasReturnPosition &&
            !string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName) &&
            scene.name == PlayerReturnContext.ReturnSceneName;

        if (!isReturnScene) return;

        StartCoroutine(CoApplyReturn());
    }

    private IEnumerator CoApplyReturn()
    {
        // --- 컨텍스트 로컬로 복사(코루틴/클리어 안정) ---
        Vector2 returnPos2 = PlayerReturnContext.ReturnPosition;

        bool useSupp = PlayerReturnContext.UseOverlapSuppression;
        float suppRadius = PlayerReturnContext.OverlapRadiusPending;
        float suppSec = PlayerReturnContext.OverlapSecondsPending;

        float gracePending = PlayerReturnContext.GraceSecondsPending;

        bool needRebind = PlayerReturnContext.CameraRebindRequested;
        string targetVcamName = PlayerReturnContext.TargetVcamName;

        bool restoreCam = PlayerReturnContext.RestoreCameraStatePending;
        CameraModeId camMode = PlayerReturnContext.ReturnCameraMode;
        float camOrtho = PlayerReturnContext.ReturnCameraOrthoSize;
        Vector2 camFixedPos = PlayerReturnContext.ReturnCameraFixedPos;
        string camBoundsName = PlayerReturnContext.ReturnCameraBoundsName;
        string enemyInstanceId = PlayerReturnContext.MonsterInstanceId;

        // 1) Player 찾기
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
                PlayerReturnContext.ClearReturnCore();
                yield break;
            }
        }

        // 2) 플레이어 이동 + delta 계산(워프 보정용)
        Vector3 fromPos = player.transform.position;
        player.transform.position = new Vector3(returnPos2.x, returnPos2.y, player.transform.position.z);

        //  추가: 워프 직후 물리(콜라이더) 동기화 → 트리거 인식 “한 박자 늦음” 완화
        Physics2D.SyncTransforms();

        Vector3 toPos = player.transform.position;
        Vector3 delta = toPos - fromPos;

        if (debugLog)
            Debug.Log($"[PlayerReturnManager] moved player => ({returnPos2.x:F2},{returnPos2.y:F2},{player.transform.position.z:F2})", this);

        // 2.5) 배틀 진입 직전 저장한 월드 오브젝트(적) 상태 복원
        TryRestoreEnemySnapshot(enemyInstanceId);

        // 3) 카메라 복원은 "1프레임 뒤" 적용
        if (needRebind || restoreCam)
        {
            StartCoroutine(CoApplyReturnCameraNextFrame(
                player.transform,
                delta,
                targetVcamName,
                restoreCam,
                camMode,
                camOrtho,
                camFixedPos,
                camBoundsName
            ));
        }

        // 4) B Suppression  (A정책에서는 배틀 복귀 저장 시 강제로 OFF로 저장됨)
        if (useSupp && suppRadius > 0f && suppSec > 0f)
        {
            SuppressNearbyTriggers((Vector2)toPos, suppRadius, suppSec);
            if (debugLog) Debug.Log($"[PlayerReturnManager] suppressed NEAR return point radius={suppRadius:F2}, sec={suppSec:F2}", this);
        }

        // 5) Grace
        float grace = gracePending > 0f ? gracePending : defaultGraceSeconds;
        if (grace > 0f) StartCoroutine(CoGrace(grace));
        else
        {
            PlayerReturnContext.IsInGracePeriod = false;
            PlayerReturnContext.GraceSecondsPending = 0f;
        }

        // 6) 1회성 데이터 정리
        PlayerReturnContext.ClearReturnCore();
    }

    private void TryRestoreEnemySnapshot(string enemyInstanceId)
    {
        if (string.IsNullOrWhiteSpace(enemyInstanceId)) return;
        if (WorldNPCStateService.Instance == null) return;

        if (!WorldNPCStateService.Instance.TryGetSnapshot(enemyInstanceId, out EnemySnapshot snap))
            return;

        var enemy = FindEnemyByInstanceId(enemyInstanceId, snap.transformPath);
        if (enemy == null)
        {
            if (debugLog)
                Debug.LogWarning($"[PlayerReturnManager] snapshot exists but enemy not found id='{enemyInstanceId}'", this);
            return;
        }

        snap.ApplyTo(enemy);
        Physics2D.SyncTransforms();

        if (debugLog)
            Debug.Log($"[PlayerReturnManager] restored enemy snapshot id='{enemyInstanceId}' pos=({snap.position.x:F2},{snap.position.y:F2})", this);
    }

    private GameObject FindEnemyByInstanceId(string instanceId, string transformPath)
    {
        var ids = FindObjectsOfType<EnemyInstanceId>(true);
        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (id != null && id.Id == instanceId)
                return id.gameObject;
        }

        // 우선순위 2: hierarchy path로 inactive 포함 탐색
        if (!string.IsNullOrWhiteSpace(transformPath))
        {
            var all = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null) continue;
                if (BuildTransformPath(t) == transformPath)
                    return t.gameObject;
            }
        }

        // fallback: EnemyInstanceId가 없는 오브젝트(트리거 연출 몬스터 등)
        var byName = GameObject.Find(instanceId);
        return byName;
    }

    private static string BuildTransformPath(Transform t)
    {
        if (t == null) return string.Empty;
        var stack = new Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack.ToArray());
    }

    private IEnumerator CoApplyReturnCameraNextFrame(
        Transform playerTr,
        Vector3 delta,
        string preferredVcamName,
        bool restoreCam,
        CameraModeId camMode,
        float camOrtho,
        Vector2 camFixedPos,
        string camBoundsName
    )
    {
        // 씬의 Start()들(=SceneCameraBootstrap) 먼저 돌게 1프레임 양보
        yield return null;

        if (!CameraManager.Instance) yield break;

        // vcam 재탐색(배틀씬에 vcam 없었다가 돌아오는 케이스)
        CameraManager.Instance.ReacquireVcam(preferredVcamName, logWhenMissing: false);

        if (!restoreCam) yield break;

        float? size = (camOrtho > 0f) ? camOrtho : (float?)null;

        // Follow 대상(기본은 Player)
        Transform follow = playerTr;

        // Confiner bounds는 “이름으로 재탐색”
        Collider2D bounds = null;
        if (!string.IsNullOrWhiteSpace(camBoundsName))
        {
            var all = FindObjectsOfType<Collider2D>(true);
            foreach (var c in all)
            {
                if (c != null && c.name == camBoundsName) { bounds = c; break; }
            }
        }

        if (debugLog)
            Debug.Log($"[PlayerReturnManager] apply return camera => mode={camMode}, ortho={(size.HasValue ? size.Value.ToString("F2") : "(default)")}, bounds='{camBoundsName}' found={(bounds ? bounds.name : "(null)")}, fixed=({camFixedPos.x:F2},{camFixedPos.y:F2})", this);

        switch(camMode) {
            case CameraModeId.Fixed:
                CameraManager.Instance.SetFixed(new Vector3(camFixedPos.x, camFixedPos.y, 0f), size);
                CameraManager.Instance.SnapCameraTo(new Vector3(camFixedPos.x, camFixedPos.y, 0f));
                break;

            case CameraModeId.Cutscene:
                CameraManager.Instance.SetCutscene(new Vector3(camFixedPos.x, camFixedPos.y, 0f), size);
                CameraManager.Instance.SnapCameraTo(new Vector3(camFixedPos.x, camFixedPos.y, 0f));
                break;

            case CameraModeId.FollowConfined:
                if(follow != null) {
                    if(bounds != null) CameraManager.Instance.SetFollowConfined(follow, bounds, size);
                    else CameraManager.Instance.SetFollowFree(follow, size); // bounds 못찾으면 FollowFree
                    CameraManager.Instance.NotifyTargetWarp(follow, delta);
                }
                break;

            case CameraModeId.FollowFree:
                if(follow != null) {
                    CameraManager.Instance.SetFollowFree(follow, size);
                    CameraManager.Instance.NotifyTargetWarp(follow, delta);
                }
                break;
        }
    }

    private void SuppressNearbyTriggers(Vector2 center, float radius, float seconds)
    {
        var cols = Physics2D.OverlapCircleAll(center, radius, overlapMask);
        if (cols == null || cols.Length == 0) return;

        var targets = new List<(TriggerGet tg, Collider2D col)>();

        foreach (var c in cols)
        {
            if (c == null) continue;

            var tg = c.GetComponent<TriggerGet>();
            if (tg == null) tg = c.GetComponentInParent<TriggerGet>();

            if (tg != null)
            {
                Collider2D toDisableCol = disableColliderAlso ? c : null;

                if (!tg.enabled && (toDisableCol == null || !toDisableCol.enabled))
                    continue;

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
