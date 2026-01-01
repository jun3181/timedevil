using UnityEngine;
using System.Collections;
using Cinemachine;

public class Cutscene_Cornered_02 : MonoBehaviour
{
    [Header("0. 시작 딜레이")]
    public float startDelay = 0.5f;

    [Header("1. 대상 연결")]
    public GameObject helper;
    public Transform[] movePath;

    [Header("2. 설정")]
    public float moveSpeed = 2.0f;
    public Dialogue dialogue;

    [Header("3. 카메라 이동 설정 (2번 카메라)")]
    public GameObject cutsceneCameraObject;
    public float cameraMoveDelay = 2.0f;

    [Header("4. 스프라이트 수동 교체")]
    public Sprite playerIdleRight; // 플레이어가 오른쪽 보는 멈춘 이미지
    public Sprite helperIdleLeft;  // 조력자가 왼쪽 보는 멈춘 이미지

    // ▼▼▼ [추가됨] 컷씬 종료 후 재생할 BGM ▼▼▼
    [Header("5. BGM 설정")]
    public AudioClip endSceneBGM;
    // ▲▲▲▲▲▲

    public void StartPart2()
    {
        StartCoroutine(CutsceneSequence());
    }

    IEnumerator CutsceneSequence()
    {
        if (cutsceneCameraObject != null) cutsceneCameraObject.SetActive(true);
        if (startDelay > 0) yield return new WaitForSeconds(startDelay);
        if (cameraMoveDelay > 0) yield return new WaitForSeconds(cameraMoveDelay);

        // --- 대화 전 스프라이트 교체 ---
        if (helper != null)
        {
            Animator helperAnim = helper.GetComponent<Animator>();
            SpriteRenderer helperSR = helper.GetComponent<SpriteRenderer>();
            if (helperAnim != null) helperAnim.enabled = false;
            if (helperSR != null && helperIdleLeft != null) helperSR.sprite = helperIdleLeft;
        }

        PlayerAction realPlayer = FindObjectOfType<PlayerAction>();
        if (realPlayer != null)
        {
            Animator playerAnim = realPlayer.GetComponent<Animator>();
            SpriteRenderer playerSR = realPlayer.GetComponent<SpriteRenderer>();
            if (playerAnim != null) playerAnim.enabled = false;
            if (playerSR != null && playerIdleRight != null) playerSR.sprite = playerIdleRight;
        }

        // --- 대화 시작 ---
        if (DialogueManager.instance != null && dialogue.sentences.Length > 0)
        {
            DialogueManager.instance.StartDialogue(dialogue);
            yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive);
        }

        // --- 조력자 이동 ---
        if (helper != null && movePath != null && movePath.Length > 0)
        {
            Animator helperAnim = helper.GetComponent<Animator>();
            SpriteRenderer helperSR = helper.GetComponent<SpriteRenderer>();

            if (helperAnim != null) helperAnim.enabled = true; // 이동 애니메이션 켬

            foreach (Transform targetPoint in movePath)
            {
                if (targetPoint == null) continue;
                while (Vector3.Distance(helper.transform.position, targetPoint.position) > 0.01f)
                {
                    helper.transform.position = Vector3.MoveTowards(
                        helper.transform.position, targetPoint.position, moveSpeed * Time.deltaTime
                    );

                    if (helperAnim != null)
                    {
                        Vector3 dir = (targetPoint.position - helper.transform.position).normalized;
                        int h = 0; int v = 0;
                        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) h = (int)Mathf.Sign(dir.x); else v = (int)Mathf.Sign(dir.y);
                        if (helperAnim.GetInteger("hAxisRaw") != h) { helperAnim.SetBool("isChange", true); helperAnim.SetInteger("hAxisRaw", h); helperAnim.SetInteger("vAxisRaw", 0); }
                        else if (helperAnim.GetInteger("vAxisRaw") != v) { helperAnim.SetBool("isChange", true); helperAnim.SetInteger("hAxisRaw", 0); helperAnim.SetInteger("vAxisRaw", v); }
                        else helperAnim.SetBool("isChange", false);
                    }
                    yield return null;
                }
            }

            // 이동 끝: 왼쪽 보기 고정
            if (helperAnim != null) helperAnim.enabled = false;
            if (helperSR != null && helperIdleLeft != null)
            {
                helperSR.sprite = helperIdleLeft;
            }
        }

        // 조력자 퇴장 및 카메라 복구
        if (helper != null) helper.SetActive(false);
        if (cutsceneCameraObject != null)
        {
            cutsceneCameraObject.SetActive(false);
            yield return new WaitForSeconds(1.5f);
        }

        // ▼▼▼ [추가됨] 컷씬 종료 시 BGM 변경 ▼▼▼
        if (endSceneBGM != null && BGMManager.instance != null)
        {
            BGMManager.instance.PlayBGM(endSceneBGM);
        }
        // ▲▲▲▲▲▲

        // 플레이어 애니메이터 복구
        if (realPlayer != null)
        {
            Animator playerAnim = realPlayer.GetComponent<Animator>();
            if (playerAnim != null) playerAnim.enabled = true;
        }

        // 게임 재개
        if (GameManager.Instance != null) GameManager.Instance.isAction = false;
        gameObject.SetActive(false);
    }
}