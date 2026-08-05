using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// BoxCollider2D로 지정한 구역 안에 프리팹을 무작위로 한 번 생성합니다.
/// 생성 전에는 같은 오브젝트를 반투명하게 표시하고 충돌 판정을 끕니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PrefabSpawner : MonoBehaviour
{
    [Header("필수 설정")]
    [SerializeField, FormerlySerializedAs("laserPrefab"), Tooltip("생성할 프리팹")]
    private GameObject prefab;

    [SerializeField, Tooltip("프리팹이 생성될 범위. 회전/크기 변경도 반영됩니다.")]
    private BoxCollider2D spawnArea;

    [Header("생성 설정")]
    [SerializeField, Min(0f), Tooltip("예고가 표시되는 시간(초)")]
    private float warningDuration = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("예고 상태의 투명도. 0.5는 50%입니다.")]
    private float warningAlpha = 0.5f;

    [SerializeField, Min(0f), Tooltip("실제 프리팹이 유지되는 시간(초). 0이면 비활성화될 때까지 유지됩니다.")]
    private float activeDuration = 1f;

    [SerializeField, FormerlySerializedAs("playOnStart"), Tooltip("컴포넌트가 활성화될 때마다 한 번 생성합니다. TriggerStep_TimedScriptRunner에서 이 컴포넌트를 켜면 자동 실행됩니다.")]
    private bool spawnOnEnable = true;

    private Coroutine spawnRoutine;
    private GameObject currentInstance;

    private void Reset()
    {
        spawnArea = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        if (spawnOnEnable)
        {
            Spawn();
        }
    }

    private void OnDisable()
    {
        StopSpawn();
    }

    /// <summary>프리팹을 한 번 생성합니다.</summary>
    public void Spawn()
    {
        if (spawnRoutine != null)
        {
            return;
        }

        if (prefab == null || spawnArea == null)
        {
            Debug.LogError($"[{nameof(PrefabSpawner)}] Prefab과 Spawn Area를 지정해야 합니다.", this);
            return;
        }

        spawnRoutine = StartCoroutine(SpawnPrefab());
    }

    /// <summary>현재 예고/프리팹을 제거합니다.</summary>
    public void StopSpawn()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }
    }

    private IEnumerator SpawnPrefab()
    {
        Vector3 position = GetRandomPosition();
        currentInstance = Instantiate(prefab, position, prefab.transform.rotation);
        SpriteRenderer[] renderers = currentInstance.GetComponentsInChildren<SpriteRenderer>(true);
        Collider2D[] colliders = currentInstance.GetComponentsInChildren<Collider2D>(true);

        var originalColors = new Color[renderers.Length];
        var colliderStates = new Dictionary<Collider2D, bool>(colliders.Length);

        // 예고 중에는 보이기만 하고 플레이어에게 피해를 주지 않습니다.
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            Color warningColor = originalColors[i];
            warningColor.a *= warningAlpha;
            renderers[i].color = warningColor;
        }

        foreach (Collider2D laserCollider in colliders)
        {
            colliderStates.Add(laserCollider, laserCollider.enabled);
            laserCollider.enabled = false;
        }

        if (warningDuration > 0f)
        {
            yield return new WaitForSeconds(warningDuration);
        }

        // 예고가 끝나면 원래 모습과 충돌 상태를 복구해 실제 프리팹으로 전환합니다.
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].color = originalColors[i];
        }

        foreach (KeyValuePair<Collider2D, bool> state in colliderStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled = state.Value;
            }
        }

        if (activeDuration <= 0f)
        {
            spawnRoutine = null;
            yield break;
        }

        yield return new WaitForSeconds(activeDuration);

        Destroy(currentInstance);
        currentInstance = null;
        spawnRoutine = null;
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 localPoint = spawnArea.offset + new Vector2(
            Random.Range(-spawnArea.size.x * 0.5f, spawnArea.size.x * 0.5f),
            Random.Range(-spawnArea.size.y * 0.5f, spawnArea.size.y * 0.5f));

        Vector3 worldPoint = spawnArea.transform.TransformPoint(localPoint);
        worldPoint.z = transform.position.z;
        return worldPoint;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnArea == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = spawnArea.transform.localToWorldMatrix;
        Gizmos.DrawCube(spawnArea.offset, spawnArea.size);
        Gizmos.matrix = oldMatrix;
    }
}
