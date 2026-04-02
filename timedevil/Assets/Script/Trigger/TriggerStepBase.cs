// Assets/Script/Trigger/TriggerStepBase.cs
using System.Collections;
using UnityEngine;

public abstract class TriggerStepBase : MonoBehaviour, ITriggerStep
{
    [Header("Input Release (Optional)")]
    [Tooltip("켜면 이 Step이 시작될 때 입력 잠금을 해제합니다.")]
    [SerializeField] private bool releaseInputLockOnStepStart = false;

    [Tooltip("켜면 컷씬 컨텍스트(TriggerContext의 trigger/router가 null)에서만 입력 잠금을 해제합니다.")]
    [SerializeField] private bool releaseOnlyWhenCutsceneContext = true;

    [Tooltip("해제 시 PlayerMove 컴포넌트를 다시 활성화합니다.")]
    [SerializeField] private bool enablePlayerMoveOnRelease = true;

    [Tooltip("true면 모든 잠금 카운트를 즉시 해제(ForceClear), false면 UnlockAction 1회만 수행")]
    [SerializeField] private bool forceClearAllActionLocks = true;

    [SerializeField] private bool debugInputReleaseLog = false;

    public abstract IEnumerator Execute(TriggerContext ctx);

    public virtual void PreExecute(TriggerContext ctx)
    {
        if (!releaseInputLockOnStepStart)
            return;

        if (releaseOnlyWhenCutsceneContext && !IsCutsceneContext(ctx))
            return;

        if (GameManager.Instance != null)
        {
            if (forceClearAllActionLocks)
                GameManager.Instance.ForceClearActionLocks();
            else
                GameManager.Instance.UnlockAction();

            if (debugInputReleaseLog)
                Debug.Log($"[{GetType().Name}] Input lock released. forceClear={forceClearAllActionLocks}");
        }
        else if (debugInputReleaseLog)
        {
            Debug.LogWarning($"[{GetType().Name}] GameManager.Instance is null (cannot release input lock)");
        }

        if (!enablePlayerMoveOnRelease)
            return;

        PlayerMove pm = ResolvePlayerMove(ctx);
        if (pm != null)
        {
            pm.enabled = true;
            if (debugInputReleaseLog)
                Debug.Log($"[{GetType().Name}] PlayerMove enabled.");
        }
    }

    private static bool IsCutsceneContext(TriggerContext ctx)
    {
        if (ctx == null) return false;
        return ctx.trigger == null && ctx.router == null;
    }

    private static PlayerMove ResolvePlayerMove(TriggerContext ctx)
    {
        if (ctx != null && ctx.playerMove != null)
            return ctx.playerMove;

        var pm = Object.FindObjectOfType<PlayerMove>(true);
        if (pm != null) return pm;

        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.GetComponent<PlayerMove>() : null;
    }
}
