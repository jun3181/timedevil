using UnityEngine;
using System.Collections;
using Cinemachine;

public class Chapter1_OpeningSequence : MonoBehaviour
{
    [Header("오프닝 독백")]
    public Dialogue openingMonologue;

    [Header("침대 오브젝트 연결")]
    public GameObject normalBedObject;
    public GameObject lyingDownBedObject;

    [Header("카메라 설정")]
    public CinemachineVirtualCamera playerFollowCamera;

    [Header("플레이어 설정")]
    public Transform wakeUpPoint;

    [Header("연출 시간 설정")]
    public float delayBeforeSleep = 2f;
    public float delayBeforeWakeUp = 2f;

    [Header("대사 자동 진행 속도")]
    public float autoNextDelay = 2f; // 한 줄 대사 끝나고 다음 대사로 넘어가는 시간

    private SpriteRenderer playerSprite;
    private PlayerAction playerAction;

    void Start()
    {
        if (PlayerReturnContext.HasReturnPosition)
        {
            this.enabled = false;
            return;
        }

        playerAction = FindObjectOfType<PlayerAction>();
        if (playerAction != null)
        {
            playerSprite = playerAction.GetComponent<SpriteRenderer>();
            playerSprite.enabled = false;
        }

        if (playerFollowCamera != null)
        {
            playerFollowCamera.Follow = null;
        }

        normalBedObject.SetActive(false);
        lyingDownBedObject.SetActive(true);

        StartCoroutine(OpeningSequence());
    }

    IEnumerator OpeningSequence()
    {
        // 대사 자동 진행 코루틴 시작
        StartCoroutine(AutoAdvanceDialogue());

        // 대사 끝날 때까지 기다림
        yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive);

        // 씬 연출
        yield return new WaitForSeconds(delayBeforeSleep);
        yield return StartCoroutine(SceneFader.instance.Fade(1f));
        yield return new WaitForSeconds(delayBeforeWakeUp);
        yield return StartCoroutine(SceneFader.instance.Fade(0f));

        if (playerAction != null && wakeUpPoint != null)
            playerAction.transform.position = wakeUpPoint.position;

        if (playerSprite != null)
            playerSprite.enabled = true;

        normalBedObject.SetActive(true);
        lyingDownBedObject.SetActive(false);

        this.enabled = false;
    }

    IEnumerator AutoAdvanceDialogue()
    {
        DialogueManager.instance.StartDialogue(openingMonologue);

        while (DialogueManager.instance.isDialogueActive)
        {
            yield return new WaitForSeconds(autoNextDelay);
            DialogueManager.instance.DisplayNextSentence();
        }
    }
}
