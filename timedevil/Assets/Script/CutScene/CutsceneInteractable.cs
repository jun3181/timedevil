// Assets/Script/Cutscene/CutsceneAutoTrigger.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CutsceneAutoTrigger : MonoBehaviour
{
    public string cutsceneId = "intro_01";
    public bool oneShot = true;
    public string playerTag = "Player";
    public bool debugLog = true;

    private bool _played = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && _played) return;
        if (!other || (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))) return;

        if (CutSceneManager.Instance == null) return;

        bool ok = CutSceneManager.Instance.Play(cutsceneId);
        if (debugLog) Debug.Log($"[CutsceneAutoTrigger] Enter -> Play('{cutsceneId}') = {ok}", this);

        if (ok) _played = true;
    }
}
