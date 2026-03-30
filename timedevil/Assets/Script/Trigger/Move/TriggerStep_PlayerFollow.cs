// Assets/Script/Trigger/Move/TriggerStep_PlayerFollow.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_PlayerFollow : TriggerStepBase
{
    [Header("Player Stop")]
    [Tooltip("실행 시 플레이어 이동을 멈춥니다.")]
    [SerializeField] private bool stopPlayerImmediately = true;

    [Tooltip("실행 시 GameManager.LockAction()으로 입력/행동을 잠급니다.")]
    [SerializeField] private bool lockPlayerAction = true;

    [Tooltip("Step 종료 시 UnlockAction() 호출")]
    [SerializeField] private bool unlockAtEnd = false;

    [Header("Active / Deactive")]
    [SerializeField] private List<GameObject> deactivateObjects = new();
    [SerializeField] private List<GameObject> activateObjects = new();

    [Header("Options")]
    [Tooltip("true면 Deactive -> Active 순서로 적용")]
    [SerializeField] private bool deactivateFirst = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        bool heldLock = false;

        if (stopPlayerImmediately)
            StopPlayer(ctx);

        if (lockPlayerAction && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            heldLock = true;
        }

        if (deactivateFirst)
        {
            ApplyActiveList(deactivateObjects, false);
            ApplyActiveList(activateObjects, true);
        }
        else
        {
            ApplyActiveList(activateObjects, true);
            ApplyActiveList(deactivateObjects, false);
        }

        if (heldLock && unlockAtEnd && GameManager.Instance != null)
            GameManager.Instance.UnlockAction();

        yield break;
    }

    private void StopPlayer(TriggerContext ctx)
    {
        PlayerMove pm = ctx != null ? ctx.playerMove : null;
        if (!pm) pm = Object.FindObjectOfType<PlayerMove>(true);

        if (pm != null)
            pm.SetMoveInput(0, 0, false, false, false, false);

        Rigidbody2D rb = null;
        if (pm != null)
            rb = pm.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (debugLog)
            Debug.Log("[TriggerStep_PlayerFollow] Player movement stopped.");
    }

    private void ApplyActiveList(List<GameObject> list, bool active)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var go = list[i];
            if (!go) continue;

            go.SetActive(active);

            if (debugLog)
                Debug.Log($"[TriggerStep_PlayerFollow] {(active ? "Activate" : "Deactivate")} -> {go.name}");
        }
    }
}
