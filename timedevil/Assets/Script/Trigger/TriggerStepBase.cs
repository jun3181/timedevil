// Assets/Script/Trigger/TriggerStepBase.cs
using System.Collections;
using UnityEngine;

public abstract class TriggerStepBase : MonoBehaviour, ITriggerStep
{
    public abstract IEnumerator Execute(TriggerContext ctx);

    /// <summary>
    /// 컷씬/트리거 도중 특정 시점에서 입력 잠금을 풀고 싶을 때 호출합니다.
    /// </summary>
    protected void ReleaseInputLock(TriggerContext ctx, bool forceClearAllLocks = true, bool enablePlayerMove = true, bool debugLog = false)
    {
        if (GameManager.Instance != null)
        {
            if (forceClearAllLocks)
                GameManager.Instance.ForceClearActionLocks();
            else
                GameManager.Instance.UnlockAction();

            if (debugLog)
                Debug.Log($"[{GetType().Name}] ReleaseInputLock(forceClearAllLocks={forceClearAllLocks})");
        }
        else if (debugLog)
        {
            Debug.LogWarning($"[{GetType().Name}] GameManager.Instance is null.");
        }

        if (!enablePlayerMove)
            return;

        PlayerMove pm = null;

        if (ctx != null)
            pm = ctx.playerMove;

        if (pm == null)
            pm = Object.FindObjectOfType<PlayerMove>(true);

        if (pm != null)
        {
            pm.enabled = true;
            if (debugLog) Debug.Log($"[{GetType().Name}] PlayerMove enabled.");
        }
    }
}
