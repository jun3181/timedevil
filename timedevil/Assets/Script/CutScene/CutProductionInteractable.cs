// Assets/Script/Cutscene/Production/CutProductionInteractable.cs
using UnityEngine;

[DisallowMultipleComponent]
public class CutProductionInteractable : MonoBehaviour, IInteractable
{
    public string key;
    public bool oneShot = true;
    public bool disableColliderAfterPlay = false;
    public bool debugLog = true;

    private bool _played;

    public void Interact()
    {
        if (oneShot && _played) return;

        var mgr = CutProductionManager.Instance;
        if (mgr == null) return;

        bool ok = mgr.Play(key, gameObject);
        if (debugLog) Debug.Log($"[CutProductionInteractable] Interact -> Play('{key}') = {ok}", this);

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
