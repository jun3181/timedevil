using UnityEngine;

[CreateAssetMenu(fileName = "New Quest NPC", menuName = "NPC Data/Quest NPC (퀘스트)")]
public class QuestNPCData : BaseNPCSO
{
    public QuestNPCData()
    {
        npcType = NPCType.Quest;
    }

    [Header("퀘스트 전용 데이터")]
    public string questID; // 연결될 퀘스트의 ID

    [Tooltip("퀘스트 진행 중에 말을 걸면 나올 대화")]
    public Dialogue processingDialogue;

    [Tooltip("퀘스트 완료 후 말을 걸면 나올 대화")]
    public Dialogue completeDialogue;
}