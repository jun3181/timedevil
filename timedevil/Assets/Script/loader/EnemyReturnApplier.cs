using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyReturnApplier : MonoBehaviour
{
    [Header("Player Ref (optional)")]
    [SerializeField] private Transform playerTransform;

    [Header("Reveal")]
    [SerializeField] private float minHiddenDelay = 0f;
    [SerializeField] private float maxHiddenDelay = 1f;

    void Start()
    {
        if (!PlayerReturnContext.HasReturnPosition) return;
        if (PlayerReturnContext.ReturnSceneName != SceneManager.GetActiveScene().name) return;

        if (playerTransform) playerTransform.position = PlayerReturnContext.ReturnPosition;

        var instanceId = PlayerReturnContext.MonsterInstanceId;
        var nameFallback = PlayerReturnContext.MonsterNameInScene;

        if (string.IsNullOrEmpty(instanceId) && string.IsNullOrEmpty(nameFallback)) return;

        GameObject enemyGo = null;
        if (!string.IsNullOrEmpty(instanceId))
        {
            var all = FindObjectsOfType<EnemyInstanceId>(true);
            foreach (var e in all)
            {
                if (e.Id == instanceId)
                {
                    enemyGo = e.gameObject;
                    break;
                }
            }
        }

        if (!enemyGo && !string.IsNullOrEmpty(nameFallback))
        {
            var cand = GameObject.Find(nameFallback);
            if (cand) enemyGo = cand;
        }

        if (!enemyGo) return;

        var p = enemyGo.transform;
        while (p != null && !p.gameObject.activeSelf)
        {
            p.gameObject.SetActive(true);
            p = p.parent;
        }

        if (WorldNPCStateService.Instance &&
            WorldNPCStateService.Instance.TryGetSnapshot(instanceId ?? nameFallback, out var snap))
        {
            snap.ApplyTo(enemyGo);
#if UNITY_EDITOR
            Debug.Log($"[EnemyReturnApplier] restored id='{snap.instanceId}' pos={snap.position}");
#endif
        }

        StartCoroutine(Co_RevealThenStart(enemyGo));
    }

    private IEnumerator Co_RevealThenStart(GameObject enemyGo)
    {
        if (!enemyGo) yield break;

        float grace = (PlayerReturnContext.IsInGracePeriod && PlayerReturnContext.GraceSecondsPending > 0f)
            ? PlayerReturnContext.GraceSecondsPending
            : 0f;

        enemyGo.SetActive(false);

        float delay = Mathf.Clamp(Random.Range(minHiddenDelay, maxHiddenDelay), 0f, maxHiddenDelay);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        enemyGo.SetActive(true);

        var mover = enemyGo.GetComponent<MonsterMover>();
        if (mover) mover.StartChase(playerTransform);

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
    }
}
