using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Header("UI Elements")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI nameText;
    public Image portraitImage;
    public TextMeshProUGUI dialogueText;

    private Queue<Sentence> sentenceQueue;
    public bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private bool isStartingDialogue = false;

    public bool blockInput = false;

    [Header("Sound")]
    public AudioSource sfxSource;   // 기존 audioSource (타이핑 효과음용)
    public AudioSource voiceSource; // ★추가됨: 성우 목소리 전용
    public AudioClip typingSound;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sentenceQueue = new Queue<Sentence>();

        // 편의상 자동으로 찾아주기 (기존 컴포넌트)
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (blockInput) return;

        if (isDialogueActive && !isStartingDialogue && Input.GetKeyDown(KeyCode.E))
        {
            DisplayNextSentence();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;
        isStartingDialogue = true;
        dialogueCanvas.SetActive(true);
        sentenceQueue.Clear();

        foreach (Sentence sentence in dialogue.sentences)
        {
            sentenceQueue.Enqueue(sentence);
        }

        StartCoroutine(StartDialogueRoutine());
    }

    private IEnumerator StartDialogueRoutine()
    {
        yield return null;
        DisplayNextSentence();
        isStartingDialogue = false;
    }

    public void DisplayNextSentence()
    {
        if (sentenceQueue.Count == 0)
        {
            StartCoroutine(EndDialogueRoutine());
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        Sentence sentence = sentenceQueue.Dequeue();
        nameText.text = sentence.characterName;

        if (portraitImage != null)
        {
            portraitImage.sprite = sentence.characterPortrait;
            if (sentence.characterPortrait == null) portraitImage.color = new Color(1, 1, 1, 0);
            else portraitImage.color = new Color(1, 1, 1, 1);
        }

        // ▼▼▼ 소리 재생 로직 분리 ▼▼▼

        // 1. 이전 목소리 끄기 (말이 겹치지 않게)
        if (voiceSource != null) voiceSource.Stop();

        // 2. 새 목소리 재생 (전용 스피커 사용)
        if (voiceSource != null && sentence.voiceClip != null)
        {
            voiceSource.PlayOneShot(sentence.voiceClip);
        }
        // ▲▲▲▲▲▲

        typingCoroutine = StartCoroutine(TypeSentence(sentence.text));
    }

    IEnumerator TypeSentence(string text)
    {
        dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;

            // 3. 타이핑 소리는 효과음 스피커로 재생
            if (sfxSource != null && typingSound != null)
                sfxSource.PlayOneShot(typingSound);

            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator EndDialogueRoutine()
    {
        yield return new WaitForEndOfFrame();

        isDialogueActive = false;
        dialogueCanvas.SetActive(false);

        // 종료 시 소리 끄기
        if (voiceSource != null) voiceSource.Stop();
        if (sfxSource != null) sfxSource.Stop();
    }
}