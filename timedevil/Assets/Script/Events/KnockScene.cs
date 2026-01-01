using UnityEngine;
using System.Collections; // 코루틴을 사용하기 위해 필요

// '똑똑똑' 소리를 내기 위해 AudioSource가 필요합니다.
[RequireComponent(typeof(AudioSource))]
public class KnockSequence : MonoBehaviour, IInteractable
{
    [Header("1. 창문에서 할 대화 (첫번째)")]
    public Dialogue windowDialogue; // ObjectInteraction에 연결했던 '창문 대화'

    [Space(10)]
    [Header("2. 문에서 일어날 이벤트 (두번째)")]
    [Tooltip("창문 대화가 끝나고 몇 초 후에 문을 두드릴지")]
    public float delayBeforeKnock = 2.0f;

    [Tooltip("'똑똑똑' 노크 소리 오디오 클립")]
    public AudioClip knockSound;

    [Tooltip("노크 소리 후 시작될 새로운 독백")]
    public Dialogue doorMonologue;

    private AudioSource audioSource;
    private bool isSequenceRunning = false; // 컷씬이 중복 실행되는 것을 방지

    void Start()
    {
        // '똑똑똑' 소리를 재생할 AudioSource 컴포넌트를 미리 가져옵니다.
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 플레이어가 E키 등으로 호출하는 진입점
    /// </summary>
    public void Interact()
    {
        // 1. 컷씬이 이미 진행 중이면 아무것도 안 함
        if (isSequenceRunning) return;

        // 2. 다른 대화가 이미 진행 중이어도(예: 오프닝 독백) 일단 대기
        if (DialogueManager.instance.isDialogueActive) return;

        // 3. 모든 조건이 맞으면 컷씬 코루틴 시작!
        StartCoroutine(FullSequence());
    }

    /// <summary>
    /// 전체 이벤트 순서(시퀀스)를 관리하는 코루틴
    /// </summary>
    IEnumerator FullSequence()
    {
        // 컷씬 시작 플래그 ON (중복 실행 방지)
        isSequenceRunning = true;

        // --- 1. 창문 대화 시작 ---
        // (DialogueManager는 DialougueManager.cs에서 가져옴)
        DialogueManager.instance.StartDialogue(windowDialogue);

        // --- 2. 창문 대화가 끝날 때까지 대기 ---
        // (DialogueManager의 isDialogueActive가 false가 될 때까지)
        yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive);

        // --- 3. 설정한 시간(2초)만큼 대기 ---
        yield return new WaitForSeconds(delayBeforeKnock);

        // --- 4. '똑똑똑' 소리 재생 ---
        if (knockSound != null)
        {
            audioSource.PlayOneShot(knockSound);
        }

        // --- 5. 문 독백(새 대화) 시작 ---
        DialogueManager.instance.StartDialogue(doorMonologue);

        // --- 6. (선택 사항) 문 독백까지 끝나길 기다리기 ---
        yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive);

        // 컷씬 종료 플래그 OFF (다시 상호작용 가능하게)
        isSequenceRunning = false;

        // ※ 만약 이 이벤트를 딱 한 번만 실행하고 싶다면,
        // 아래 코드의 주석을 해제해서 이 스크립트(또는 오브젝트)를 비활성화하세요.
        // this.enabled = false; 
    }
}