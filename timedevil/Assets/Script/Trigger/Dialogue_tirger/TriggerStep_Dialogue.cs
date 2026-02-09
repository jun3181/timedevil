// Assets/Script/Trigger/Steps/TriggerStep_Dialogue.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_Dialogue : TriggerStepBase
{
    [Header("Dialogue")]
    [SerializeField] private Dialogue dialogue;

    [Header("Flow")]
    [Tooltip("true면 대화가 끝날 때까지 다음 Step으로 넘어가지 않음")]
    [SerializeField] private bool waitUntilDone = true;

    [Header("Input Lock")]
    [Tooltip("대화 진행 중 플레이어 입력을 막을지(GameManager LockAction 사용)")]
    [SerializeField] private bool lockPlayerInput = true;

    [Header("DialogueManager Policy")]
    [Tooltip("true면 월드 입력(E로 넘김)을 막고(=blockInput), 아래 AutoAdvance로만 넘깁니다.")]
    [SerializeField] private bool blockWorldAdvance = false;

    [Header("Auto Advance (blockWorldAdvance=true일 때만 의미 있음)")]
    [SerializeField] private bool autoAdvance = false;
    [Min(0.01f)][SerializeField] private float autoDelay = 1.5f;

    private bool _heldLock = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (dialogue == null) yield break;

        var dm = DialogueManager.instance;
        if (dm == null) yield break;

        // (선택) 입력 잠금
        if (lockPlayerInput && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            _heldLock = true;
        }

        // blockInput을 StartDialogue 전에 켜면 "첫 줄 출력"이 막히므로,
        // StartDialogue는 항상 blockInput=false 상태에서 먼저 호출
        bool prevBlock = dm.blockInput;
        dm.blockInput = false;

        dm.StartDialogue(dialogue);

        // 이후 정책 적용
        dm.blockInput = blockWorldAdvance;

        // blockWorldAdvance=true면, 플레이어 E로는 못 넘기니까
        // autoAdvance=true일 때만 여기서 자동으로 넘겨줌
        if (blockWorldAdvance && autoAdvance)
        {
            while (dm != null && dm.isDialogueActive)
            {
                // 타이핑 중이면 먼저 완성
                if (dm.IsTyping) dm.ForceCompleteTyping();
                else dm.Cutscene_DisplayNextSentence();

                yield return new WaitForSecondsRealtime(autoDelay);
            }
        }
        else if (waitUntilDone)
        {
            while (dm != null && dm.isDialogueActive)
                yield return null;
        }

        // 원복
        if (dm != null) dm.blockInput = prevBlock;

        if (_heldLock && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _heldLock = false;
        }
    }
}
