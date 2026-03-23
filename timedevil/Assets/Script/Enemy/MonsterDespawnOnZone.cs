using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MonsterDespawnOnZone : MonoBehaviour
{
    [Header("Despawn Zones")]
    [SerializeField] private List<Collider2D> despawnZones = new();

    [Header("Options")]
    [SerializeField] private bool deactivateOnDespawn = true;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool debugLog = false;

    private bool hasDespawned;

    public void Configure(IList<Collider2D> zones, bool deactivate, bool enableDebugLog)
    {
        despawnZones.Clear();

        if (zones != null)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone != null)
                    despawnZones.Add(zone);
            }
        }

        deactivateOnDespawn = deactivate;
        debugLog = enableDebugLog;
        hasDespawned = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDespawn(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDespawn(collision.collider);
    }

    private void TryDespawn(Collider2D other)
    {
        if (hasDespawned && triggerOnce)
            return;

        if (other == null || despawnZones == null || despawnZones.Count == 0)
            return;

        for (int i = 0; i < despawnZones.Count; i++)
        {
            var zone = despawnZones[i];
            if (zone == null)
                continue;

            if (!IsMatchingZone(zone, other))
                continue;

            hasDespawned = true;

            if (debugLog)
                Debug.Log($"[MonsterDespawnOnZone] Despawn '{name}' by zone '{zone.name}'");

            var mover = GetComponent<UndeadMover>();
            if (mover != null)
                mover.StopPatrol();

            if (deactivateOnDespawn)
                gameObject.SetActive(false);
            else
                Destroy(gameObject);

            return;
        }
    }

    private static bool IsMatchingZone(Collider2D configuredZone, Collider2D other)
    {
        if (configuredZone == other)
            return true;

        var configuredTransform = configuredZone.transform;
        var otherTransform = other.transform;

        return otherTransform.IsChildOf(configuredTransform) || configuredTransform.IsChildOf(otherTransform);
    }
}
