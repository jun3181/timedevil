using UnityEngine;
using System.Collections;
using Cinemachine;

public class BedCutscene : MonoBehaviour, IInteractable
{
    [Header("1. 이벤트 순서별 대화")]
    public Dialogue[] dialogues;

    [Header("2. 카메라 및 효과")]
    public CinemachineVirtualCamera virtualCamera;
    public AudioClip thumpSound;
    public string nextSceneName;

    [Header("3. 자동대사 넘김 설정")]
    public float autoAdvanceDelay = 1.5f;

    private AudioSource audioSource;
    private bool isRunning = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Interact()
    {
        if (isRunning) return;
        isRunning = true;

        // 컷씬 동안 대사 스킵 키 입력 금지
        DialogueManager.instance.blockInput = true;

        StartCoroutine(RunAllSequences());
    }

    private IEnumerator RunAllSequences()
    {
        for (int i = 0; i < dialogues.Length; i++)
        {
            float targetZoom = 5f;
            if (i == 1) targetZoom = 4f;
            else if (i == 2) targetZoom = 3f;
            else if (i == 3) targetZoom = 2f;

            yield return StartCoroutine(FadeZoomDialogue(dialogues[i], targetZoom));
        }

        yield return StartCoroutine(FinalSequence());
    }

    IEnumerator FadeZoomDialogue(Dialogue dialogue, float targetZoomSize)
    {
        yield return StartCoroutine(SceneFader.instance.Fade(1f));

        if (virtualCamera != null)
            virtualCamera.m_Lens.OrthographicSize = targetZoomSize;

        DialogueManager.instance.StartDialogue(dialogue);

        yield return StartCoroutine(SceneFader.instance.Fade(0f));

        while (DialogueManager.instance.isDialogueActive)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            DialogueManager.instance.DisplayNextSentence(); // 자동 진행
        }
    }

    IEnumerator FinalSequence()
    {
        yield return StartCoroutine(SceneFader.instance.Fade(1f));

        // 컷씬 종료 → 다시 입력 허용
        DialogueManager.instance.blockInput = false;

        SceneFader.instance.LoadSceneWithFade(nextSceneName);
    }
}
    