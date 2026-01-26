// Assets/Script/Trigger/teleport/TriggerStep_PlayerTeleport.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_PlayerTeleport : TriggerStepBase
{
    [Header("Teleport Target")]
    [SerializeField] private Transform targetPoint;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Fade (optional)")]
    [SerializeField] private bool useFade = false;

    [Header("Input Lock")]
    [SerializeField] private bool lockPlayerInput = true;

    [Header("After Teleport Camera Mode")]
    [SerializeField] private CameraModeId afterMode = CameraModeId.FollowConfined;

    [Tooltip("FollowConfined일 때 Clamp bounds로 쓸 Collider2D(보통 BoxCollider2D 추천)")]
    [SerializeField] private Collider2D afterBounds;

    [Tooltip("0이면 변경 안 함")]
    [SerializeField] private float afterOrthoSize = 0f;

    [Header("Camera Warp Fix")]
    [SerializeField] private bool notifyWarpToCinemachine = true;
    [SerializeField] private bool snapCameraWhenFixed = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!targetPoint)
        {
            Debug.LogWarning("[TriggerStep_PlayerTeleport] targetPoint가 비어있습니다.");
            yield break;
        }

        // ctx.player 가 Transform 이므로 Transform으로 받는다
        Transform playerTr = ctx != null ? ctx.player : null;

        // 혹시 ctx.player가 비어있을 때 폴백
        if (!playerTr)
        {
            var pm = Object.FindObjectOfType<PlayerMove>(true);
            playerTr = pm ? pm.transform : null;
        }

        if (!playerTr)
        {
            Debug.LogWarning("[TriggerStep_PlayerTeleport] 플레이어 Transform을 찾지 못했습니다.");
            yield break;
        }

        GameObject playerGo = playerTr.gameObject;

        Vector3 from = playerTr.position;
        Vector3 to = targetPoint.position + (Vector3)offset;

        if (debugLog)
            Debug.Log($"[TriggerStep_PlayerTeleport] Player='{playerGo.name}' from={from} to={to} mode={afterMode}");

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = true;

        if (CameraManager.Instance) CameraManager.Instance.BeginTransition(lockCamera: true);

        if (useFade && SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(1f));

        // 이동
        playerTr.position = to;

        // 워프 보정(예전 위치로 끌려가는 현상 방지)
        if (notifyWarpToCinemachine && CameraManager.Instance != null)
        {
            Vector3 delta = to - from;
            CameraManager.Instance.NotifyTargetWarp(playerTr, delta);
        }

        // 카메라 모드 적용
        if (CameraManager.Instance != null)
        {
            float? size = (afterOrthoSize > 0f) ? afterOrthoSize : (float?)null;

            switch (afterMode)
            {
                case CameraModeId.Fixed:
                    CameraManager.Instance.SetFixed(lockWorldPos: to, orthoSize: size);
                    if (snapCameraWhenFixed) CameraManager.Instance.SnapCameraTo(to);
                    break;

                case CameraModeId.FollowConfined:
                    CameraManager.Instance.SetFollowConfined(playerTr, afterBounds, size);
                    break;

                case CameraModeId.FollowFree:
                    CameraManager.Instance.SetFollowFree(playerTr, size);
                    break;

                case CameraModeId.Cutscene:
                    CameraManager.Instance.SetCutscene(to, size);
                    break;
            }

            CameraManager.Instance.EndTransition();
        }

        if (useFade && SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(0f));

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = false;
    }
}
