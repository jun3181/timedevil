// Assets/Script/CutScene/CutSceneInteraction.cs
using UnityEngine;

[DisallowMultipleComponent]
public class CutSceneInteraction : MonoBehaviour, IInteractable
{
    public string cutsceneId = "intro_01";
    public bool oneShot = true;
    public bool debugLog = true;

    private bool _used = false;

    public void Interact()
    {
        if (_used && oneShot) return;

        bool ok = (CutSceneManager.Instance != null) && CutSceneManager.Instance.Play(cutsceneId);
        if (debugLog) Debug.Log($"[CutSceneInteraction] Interact -> Play('{cutsceneId}') = {ok}", this);

        if (ok && oneShot)
        {
            _used = true;
            // 필요하면 비활성화
            // gameObject.SetActive(false);
        }
    }
}
