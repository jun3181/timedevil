// Assets/Script/Save/SavePointInteractable.cs
using UnityEngine;

public class SavePointInteractable : MonoBehaviour, IInteractable
{
    [Header("Optional SFX")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip saveClip;

    [Header("Progress Override (optional)")]
    [Tooltip("progress.json에 저장할 씬 이름을 강제로 지정. 비우면 현재 씬 이름 저장")]
    [SerializeField] private bool overrideSceneName = false;
    [SerializeField] private string sceneNameToSave = "";

    [Tooltip("플레이어 위치 대신 이 포인트 위치를 저장하고 싶으면 체크")]
    [SerializeField] private bool overridePlayerPos = false;
    [SerializeField] private Transform playerPosOverride;

    [Header("Cutscene Key (optional)")]
    [Tooltip("저장할 때 '키를 받았다' 플래그를 progress.json에 추가")]
    [SerializeField] private bool addCutsceneKeyFlag = false;
    [SerializeField] private string cutsceneKey = "CutScene1"; // 씬마다 늘어날 키

    [Header("Camera Save (inspector-defined)")]
    [SerializeField] private bool overrideCamera = true;
    [SerializeField] private CameraModeId cameraMode = CameraModeId.FollowFree;
    [SerializeField] private float cameraOrthoSize = 0f;

    [Tooltip("Fixed/Cutscene일 때 저장할 카메라 고정 위치(비우면 이 오브젝트 위치 사용)")]
    [SerializeField] private Transform fixedCameraPosOverride;

    [Tooltip("FollowConfined일 때 저장할 bounds (이름으로 저장됨)")]
    [SerializeField] private Collider2D confinerBoundsOverride;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public void Interact()
    {
        // (선택) 대화 중 저장 막기
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        var req = new SaveSystem.SaveRequest();

        // 씬 이름
        req.overrideSceneName = overrideSceneName;
        req.sceneName = sceneNameToSave;

        // 좌표
        req.overridePlayerPos = overridePlayerPos;
        req.playerPos = playerPosOverride ? playerPosOverride.position : transform.position;

        // 키/플래그
        req.addFlag = addCutsceneKeyFlag;
        req.flagKey = cutsceneKey;

        // 카메라
        req.overrideCamera = overrideCamera;
        req.cameraMode = cameraMode;
        req.cameraOrthoSize = cameraOrthoSize;

        Vector3 fixedPos = fixedCameraPosOverride ? fixedCameraPosOverride.position : transform.position;
        req.cameraFixedPos = fixedPos;

        req.cameraBoundsName = confinerBoundsOverride ? confinerBoundsOverride.name : "";

        SaveSystem.SaveAll(req);

        if (sfx && saveClip) sfx.PlayOneShot(saveClip);
        if (debugLog) Debug.Log($"[SavePoint] Saved! key='{(addCutsceneKeyFlag ? cutsceneKey : "(none)")}'");
    }
}