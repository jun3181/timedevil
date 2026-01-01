using UnityEngine;

public enum NPCType
{
    General, // 일반 주민
    Quest   // 퀘스트 NPC
}

public enum DialogueInitiator
{
    NPC,    // NPC가 먼저 말을 건다                    
    Player  // 플레이어가 먼저 말을 건다
}

public class BaseNPCSO: ScriptableObject
{
    [Header("신원 정보 (Identity)")]
    [Tooltip("시스템 내부에서 식별할 고유 ID)")]
    public string npcID;

    [Tooltip("게임 내 대화창이나 머리 위에 표시될 이름")]
    public string npcName;

    [Tooltip("NPC Type")]
    [HideInInspector] public NPCType npcType;

    [Header("비주얼 (Visual)")]
    [Tooltip("맵 상에 돌아다닐 때 보일 스프라이트")]
    public Sprite fieldSprite;

    [Tooltip("대화창에 띄울 얼굴 초상화")]
    public Sprite portrait;

    [Tooltip("걷기, 대기 모션을 담당할 애니메이션 컨트롤러")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("누가 대화를 시작하는가?")]
    public DialogueInitiator initiator = DialogueInitiator.NPC;

    [Header("기본 상호작용 (Base Interaction)")]
    [Tooltip("플레이어가 말을 걸었을 때 출력될 기본 대화")]
    public Dialogue defaultDialogue;

    [Header("이동 설정 (Movement)")]
    [Tooltip("이동 속도 (0이면 제자리에 고정)")]
    public float moveSpeed = 2.0f;
}