using UnityEngine;

[CreateAssetMenu(fileName = "New General NPC", menuName = "NPC Data/General (일반 주민)")]
public class GeneralNPCData : BaseNPCSO 
{
    // 생성자: 파일 만들 때 자동으로 타입 설정
    public GeneralNPCData()
    {
        npcType = NPCType.General;
    }

    [Header("일반 주민 추가 설정")]
    [TextArea] public string jobDescription; // (선택) 직업이나 특징 메모
}