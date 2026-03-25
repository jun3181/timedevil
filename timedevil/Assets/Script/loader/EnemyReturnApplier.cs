// Assets/Script/loader/EnemyReturnApplier.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyReturnApplier : MonoBehaviour
{
    [Header("Player Ref (선택, 무적 처리에 사용)")]
    [SerializeField] private Transform playerTransform;

    [Header("Reveal (비활성 대기 → 재활성)")]
    [SerializeField] private float minHiddenDelay = 0f;   // 0~1초 랜덤 지연
    [SerializeField] private float maxHiddenDelay = 1f;

    void Start()
    {
        // 복귀 대상 씬인지 확인
        if (!PlayerReturnContext.HasReturnPosition) return;
        if (PlayerReturnContext.ReturnSceneName != SceneManager.GetActiveScene().name) return;

        // 1) 플레이어 위치 복원
        if (playerTransform) playerTransform.position = PlayerReturnContext.ReturnPosition;

        // 2) 적 스냅샷 복원 대상 찾기
        var instanceId = PlayerReturnContext.MonsterInstanceId;
        var nameFallback = PlayerReturnContext.MonsterNameInScene;

        if (string.IsNullOrEmpty(instanceId) && string.IsNullOrEmpty(nameFallback)) return;

        GameObject enemyGo = null;
        if (!string.IsNullOrEmpty(instanceId))
        {
            var all = FindObjectsOfType<EnemyInstanceId>(true);
            foreach (var e in all) if (e.Id == instanceId) { enemyGo = e.gameObject; break; }
        }
        if (!enemyGo && !string.IsNullOrEmpty(nameFallback))
        {
            var cand = GameObject.Find(nameFallback);
            if (cand) enemyGo = cand;
        }
        if (!enemyGo) return;

        // (0) 상위 비활성 부모도 전부 활성화
        var p = enemyGo.transform;
        while (p != null && !p.gameObject.activeSelf)
        {
            p.gameObject.SetActive(true);
            p = p.parent;
        }

        // (1) 스냅샷 적용
        if (WorldNPCStateService.Instance &&
            WorldNPCStateService.Instance.TryGetSnapshot(instanceId ?? nameFallback, out var snap))
        {
            snap.ApplyTo(enemyGo);
#if UNITY_EDITOR
            Debug.Log($"[EnemyReturnApplier] restored id='{snap.instanceId}' pos={snap.position}");
#endif
        }

        // (2) 비활성 → (지연) → 재활성 → 순찰 시작 + 충돌 무적
        StartCoroutine(Co_RevealThenStart(enemyGo));
    }

    private IEnumerator Co_RevealThenStart(GameObject enemyGo)
    {
        if (!enemyGo) yield break;

        // 전투 복귀 직후 재충돌 방지시간을 이후에 적용해야 하므로 로컬에 읽어두기
        float grace = (PlayerReturnContext.IsInGracePeriod && PlayerReturnContext.GraceSecondsPending > 0f)
            ? PlayerReturnContext.GraceSecondsPending : 0f;

        // 0) 잠깐 비활성
        enemyGo.SetActive(false);

        // 1) 0~1초(설정 범위) 랜덤 대기
        float delay = Mathf.Clamp(Random.Range(minHiddenDelay, maxHiddenDelay), 0f, maxHiddenDelay);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // 2) 재활성
        enemyGo.SetActive(true);

        // 3) 순찰 재개
        var mover = enemyGo.GetComponent<UndeadMover>();
        if (mover) mover.StartPatrol();

        // 4) 재활성 직후 충돌 무적 처리(플레이어/적 콜라이더 잠시 OFF)
        if (grace > 0f)
            yield return StartCoroutine(CoTempDisableColliders(enemyGo, grace));
    }

    private IEnumerator CoTempDisableColliders(GameObject enemyGo, float sec)
    {
        var playerCol = playerTransform ? playerTransform.GetComponent<Collider2D>() : null;
        Collider2D enemyCol = enemyGo ? enemyGo.GetComponent<Collider2D>() : null;

        bool pWas = playerCol ? playerCol.enabled : false;
        bool eWas = enemyCol ? enemyCol.enabled : false;

        if (playerCol) playerCol.enabled = false;
        if (enemyCol) enemyCol.enabled = false;

        yield return new WaitForSeconds(sec);

        if (playerCol) playerCol.enabled = pWas;
        if (enemyCol) enemyCol.enabled = eWas;

        // 무적 플래그 클리어는 SceneLoaderHost 쪽 코루틴이 이미 처리 중일 것(중복 무해)
    }
}
