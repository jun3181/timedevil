// Assets/Script/Cutscene/Production/CutProductionTrigger2D.cs
using UnityEngine;

[DisallowMultipleComponent]
public class CutProductionTrigger2D : MonoBehaviour
{
    public string key;
    public bool oneShot = true;
    public string playerTag = "Player";
    public bool disableColliderAfterPlay = true;
    public bool debugLog = true;

    private bool _played;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && _played) return;
        if (!other.CompareTag(playerTag)) return;

        var mgr = CutProductionManager.Instance;
        if (mgr == null) return;

        bool ok = mgr.Play(key, other.gameObject);
        if (debugLog) Debug.Log($"[CutProductionTrigger2D] Enter -> Play('{key}') = {ok}", this);

        if (ok && oneShot)
        {
            _played = true;
            if (disableColliderAfterPlay)
            {
                var col = GetComponent<Collider2D>();
                if (col) col.enabled = false;
            }
        }
    }
}
