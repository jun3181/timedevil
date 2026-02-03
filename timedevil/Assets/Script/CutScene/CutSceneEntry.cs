// Assets/Script/CutScene/CutSceneEntry.cs
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class CutSceneEntry : MonoBehaviour
{
    [Header("Key")]
    public string cutsceneId = "intro_01";

    [Header("Actions")]
    public bool playTimeline = true;   // 작동1
    public bool playDialogue = true;   // 작동2

    [Header("Timeline")]
    public PlayableDirector director;
    public bool keepEndState = true;   // Hold 유지(끝 포즈 유지)

    [Header("Dialogue")]
    public Dialogue dialogue;

    [Header("Lock")]
    [Tooltip("컷씬 시작~완료까지 GameManager.isAction 잠금(플레이어 이동/상호작용 막기)")]
    public bool lockPlayerInput = true;

    [Header("One Shot")]
    public bool oneShot = true;

    [Header("Debug")]
    public bool debugLog = true;

    [HideInInspector] public bool played = false;

    private void Reset()
    {
        director ??= GetComponent<PlayableDirector>();
    }

    public bool IsValid(out string reason)
    {
        if (string.IsNullOrWhiteSpace(cutsceneId))
        {
            reason = "cutsceneId empty";
            return false;
        }

        if (playTimeline)
        {
            if (!director)
            {
                reason = "director missing";
                return false;
            }
            if (director.playableAsset == null)
            {
                reason = "director.playableAsset missing";
                return false;
            }
        }

        if (playDialogue)
        {
            if (dialogue == null)
            {
                reason = "dialogue missing";
                return false;
            }
        }

        reason = "";
        return true;
    }

    public void ApplyDirectorOptions()
    {
        if (!director) return;
        director.extrapolationMode = keepEndState ? DirectorWrapMode.Hold : DirectorWrapMode.None;
    }
}
