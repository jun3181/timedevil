// Assets/Script/Trigger/Steps/TriggerStep_PlayerSetActive.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerActiveOp
{
    Deactivate,
    Activate,
    DeactivateForSeconds
}

public enum ActiveTargetScope
{
    Player,
    Objects,
    PlayerAndObjects
}

[DisallowMultipleComponent]
public class TriggerStep_PlayerSetActive : TriggerStepBase
{
    [Header("Operation")]
    [SerializeField] private PlayerActiveOp op = PlayerActiveOp.DeactivateForSeconds;

    [Header("Target")]
    [SerializeField] private ActiveTargetScope targetScope = ActiveTargetScope.Player;
    [Tooltip("targetScope가 Objects 또는 PlayerAndObjects일 때 활성/비활성할 오브젝트")]
    [SerializeField] private List<GameObject> targetObjects = new();

    [Tooltip("op=DeactivateForSeconds 일 때만 사용")]
    [Min(0f)][SerializeField] private float seconds = 0.5f;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Game Lock (optional)")]
    [Tooltip("비활성화 동안 입력/행동을 잠글지")]
    [SerializeField] private bool lockAction = true;

    [Tooltip("이 Step 종료 시 UnlockAction 호출")]
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
        // Player 대상일 때만 Service 확보
        PlayerActiveService svc = null;
        if (ShouldAffectPlayer())
        {
            svc = PlayerActiveService.Instance;
            if (svc == null)
            {
                var go = new GameObject("PlayerActiveService");
                svc = go.AddComponent<PlayerActiveService>();
            }
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
                ApplyActive(false, svc);
                break;

            case PlayerActiveOp.Activate:
                if (debugLog) Debug.Log("[TriggerStep_PlayerSetActive] Activate");
                ApplyActive(true, svc);
                yield return PostEnableFix();
                break;

            case PlayerActiveOp.DeactivateForSeconds:
                if (debugLog) Debug.Log($"[TriggerStep_PlayerSetActive] DeactivateForSeconds {seconds:0.###}s");

                ApplyActive(false, svc);

                if (seconds > 0f)
                    yield return WaitSeconds(seconds, useUnscaledTime);

                ApplyActive(true, svc);
                yield return PostEnableFix();
                break;
        }

        if (held && unlockAtEnd && GameManager.Instance != null)
            GameManager.Instance.UnlockAction();
    }

    private bool ShouldAffectPlayer()
    {
        return targetScope == ActiveTargetScope.Player || targetScope == ActiveTargetScope.PlayerAndObjects;
    }

    private bool ShouldAffectObjects()
    {
        return targetScope == ActiveTargetScope.Objects || targetScope == ActiveTargetScope.PlayerAndObjects;
    }

    private void ApplyActive(bool active, PlayerActiveService svc)
    {
        if (ShouldAffectPlayer() && svc != null)
        {
            svc.SetActive(active, syncPhysics: active && syncPhysicsAfterEnable, resetVelocity: resetVelocity, clearMoveInput: clearMoveInput);
        }

        if (!ShouldAffectObjects() || targetObjects == null)
            return;

        for (int i = 0; i < targetObjects.Count; i++)
        {
            var go = targetObjects[i];
            if (!go) continue;
            go.SetActive(active);

            if (debugLog)
                Debug.Log($"[TriggerStep_PlayerSetActive] Object {(active ? "Activate" : "Deactivate")} -> {go.name}");
        }
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
