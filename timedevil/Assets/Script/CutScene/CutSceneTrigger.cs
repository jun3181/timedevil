// Assets/Script/CutScene/CutSceneTrigger.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class CutSceneTrigger : MonoBehaviour
{
    public string cutsceneId = "intro_01";
    public bool oneShot = true;
    public string playerTag = "Player";
    public bool debugLog = true;

    private bool _fired = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_fired && oneShot) return;
        if (!other.CompareTag(playerTag)) return;

        bool ok = (CutSceneManager.Instance != null) && CutSceneManager.Instance.Play(cutsceneId);
        if (debugLog) Debug.Log($"[CutSceneTrigger] Enter -> Play('{cutsceneId}') = {ok}", this);

        if (ok && oneShot)
        {
            _fired = true;
            // 재진입 방지(원하는 방식으로 하나만 택)
            GetComponent<Collider2D>().enabled = false;
            // gameObject.SetActive(false);
        }
    }
}
