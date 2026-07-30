using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxCollider2D로 지정한 구역 안에 레이저를 무작위로 생성합니다.
/// 생성 전에는 같은 오브젝트를 반투명하게 표시하고 충돌 판정을 끕니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LaserSpawner : MonoBehaviour
{
    [Header("필수 설정")]
    [SerializeField, Tooltip("생성할 레이저 프리팹")]
    private GameObject laserPrefab;

    [SerializeField, Tooltip("레이저가 생성될 범위. 회전/크기 변경도 반영됩니다.")]
    private BoxCollider2D spawnArea;

    [Header("생성 설정")]
    [SerializeField, Min(0f), Tooltip("예고가 표시되는 시간(초)")]
    private float warningDuration = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("예고 상태의 투명도. 0.5는 50%입니다.")]
    private float warningAlpha = 0.5f;

    [SerializeField, Min(0f), Tooltip("실제 레이저가 유지되는 시간(초)")]
    private float activeDuration = 1f;

    [SerializeField, Min(0f), Tooltip("레이저가 사라진 뒤 다음 예고까지의 시간(초)")]
    private float spawnInterval = 0.5f;

    [SerializeField, Tooltip("활성화되면 시작과 동시에 반복 생성을 시작합니다.")]
    private bool playOnStart = true;

    [SerializeField, Tooltip("레이저의 Z축 각도를 매번 무작위로 정합니다.")]
    private bool randomizeRotation;

    private Coroutine spawnRoutine;
    private GameObject currentLaser;

    private void Reset()
    {
        spawnArea = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartSpawning();
        }
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    /// <summary>레이저 반복 생성을 시작합니다.</summary>
    public void StartSpawning()
    {
        if (spawnRoutine != null)
        {
            return;
        }

        if (laserPrefab == null || spawnArea == null)
        {
            Debug.LogError($"[{nameof(LaserSpawner)}] Laser Prefab과 Spawn Area를 지정해야 합니다.", this);
            return;
        }

        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>반복 생성을 멈추고 현재 예고/레이저를 제거합니다.</summary>
    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (currentLaser != null)
        {
            Destroy(currentLaser);
            currentLaser = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return SpawnLaser();

            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    private IEnumerator SpawnLaser()
    {
        Vector3 position = GetRandomPosition();
        Quaternion rotation = laserPrefab.transform.rotation;
        if (randomizeRotation)
        {
            rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        currentLaser = Instantiate(laserPrefab, position, rotation);
        SpriteRenderer[] renderers = currentLaser.GetComponentsInChildren<SpriteRenderer>(true);
        Collider2D[] colliders = currentLaser.GetComponentsInChildren<Collider2D>(true);

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

        // 예고가 끝나면 원래 모습과 충돌 상태를 복구해 실제 레이저로 전환합니다.
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

        if (activeDuration > 0f)
        {
            yield return new WaitForSeconds(activeDuration);
        }

        Destroy(currentLaser);
        currentLaser = null;
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
