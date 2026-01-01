using System.Collections;
using UnityEngine;

public class SceneStartDialogue : MonoBehaviour
{
    public Dialogue dialogue;
    public bool autoAdvance = true;  // 끄고 싶으면 Inspector에서 false

    public float autoDelay = 1.5f;   // 문장 자동 넘김 딜레이

    private void Start()
    {
        DialogueManager.instance.StartDialogue(dialogue);

        if (autoAdvance)
        {
            StartCoroutine(AutoAdvanceRoutine());
        }
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        // 첫 문장 출력 직후 바로 코루틴이 실행되므로 약간 기다리는 것이 안전
        yield return new WaitForSeconds(autoDelay);

        // 대사가 활성 상태일 때만 자동 넘김
        if (DialogueManager.instance.isDialogueActive)
        {
            DialogueManager.instance.DisplayNextSentence();
        }
    }
}
