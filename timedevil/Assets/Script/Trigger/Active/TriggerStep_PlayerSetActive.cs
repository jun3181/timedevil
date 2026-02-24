// Assets/Script/Trigger/Steps/TriggerStep_PlayerSetActive.cs
using System.Collections;
using UnityEngine;

public enum PlayerActiveOp
{
    Deactivate,
    Activate,
    DeactivateForSeconds
}

[DisallowMultipleComponent]
public class TriggerStep_PlayerSetActive : TriggerStepBase
{
    [Header("Operation")]
    [SerializeField] private PlayerActiveOp op = PlayerActiveOp.DeactivateForSeconds;

    [Tooltip("op=DeactivateForSeconds 일 때만 사용")]
    [Min(0f)][SerializeField] private float seconds = 0.5f;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Game Lock (optional)")]
    [Tooltip("비활성화 중 입력/행동까지 막고 싶으면 켬")]
    [SerializeField] private bool lockAction = true;

    [Tooltip("이 Step 끝에서 UnlockAction 할지")]
    [SerializeField] private bool unlockAtEnd = true;

    [Header("Physics Fix")]
    [SerializeField] private bool syncPhysicsAfterEnable = true;
    [SerializeField] private bool waitOneFrameAfterEnable = true;
    [SerializeField] private bool waitFixedUpdateAfterEnable = false;

    [Header("Player Motion Fix")]
    [SerializeField] private bool resetVelocity = true;
    [SerializeField] private bool clearMoveInput = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        // Service 확보(없으면 만들어도 됨)
        var svc = PlayerActiveService.Instance;
        if (svc == null)
        {
            var go = new GameObject("PlayerActiveService");
            svc = go.AddComponent<PlayerActiveService>();
        }

        bool held = false;
        if (lockAction && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            held = true;
        }

        switch (op)
        {
            case PlayerActiveOp.Deactivate:
                if (debugLog) Debug.Log("[TriggerStep_PlayerSetActive] Deactivate");
                svc.SetActive(false, syncPhysics: false, resetVelocity: resetVelocity, clearMoveInput: clearMoveInput);
                break;

            case PlayerActiveOp.Activate:
                if (debugLog) Debug.Log("[TriggerStep_PlayerSetActive] Activate");
                svc.SetActive(true, syncPhysics: syncPhysicsAfterEnable, resetVelocity: resetVelocity, clearMoveInput: clearMoveInput);
                yield return PostEnableFix();
                break;

            case PlayerActiveOp.DeactivateForSeconds:
                if (debugLog) Debug.Log($"[TriggerStep_PlayerSetActive] DeactivateForSeconds {seconds:0.###}s");

                svc.SetActive(false, syncPhysics: false, resetVelocity: resetVelocity, clearMoveInput: clearMoveInput);

                if (seconds > 0f)
                    yield return WaitSeconds(seconds, useUnscaledTime);

                svc.SetActive(true, syncPhysics: syncPhysicsAfterEnable, resetVelocity: resetVelocity, clearMoveInput: clearMoveInput);
                yield return PostEnableFix();
                break;
        }

        if (held && unlockAtEnd && GameManager.Instance != null)
            GameManager.Instance.UnlockAction();
    }

    private IEnumerator PostEnableFix()
    {
        if (waitOneFrameAfterEnable) yield return null;
        if (waitFixedUpdateAfterEnable) yield return new WaitForFixedUpdate();
    }

    private IEnumerator WaitSeconds(float sec, bool unscaled)
    {
        if (sec <= 0f) yield break;

        if (unscaled)
            yield return new WaitForSecondsRealtime(sec);
        else
            yield return new WaitForSeconds(sec);
    }
}
