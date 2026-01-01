using UnityEngine;
using System.Collections;
using Cinemachine;

[RequireComponent(typeof(BoxCollider2D))]
public class Cutscene_Cornered_01 : MonoBehaviour
{
    [Header("1. 컷씬에 등장할 대상")]
    public PlayerAction player;
    public GameObject fakePlayerActor;
    public GameObject monster;
    public GameObject helper;

    [Header("2. 이동할 목표 지점")]
    public Transform playerTargetPoint;
    public Transform monsterTargetPoint;
    public Transform helperSpawnPoint;
    public Transform helperTargetPoint;

    [Header("3. 이동 속도 설정")]
    public float playerMoveSpeed = 2f;
    public float monsterMoveSpeed = 1.5f;
    public float helperMoveSpeed = 2.5f;

    [Header("4. 컷씬 중 대화")]
    public Dialogue[] dialogues;

    [Header("5. 다음 컷씬 연결")]
    public Cutscene_Cornered_02 nextCutscenePart;
    public float transitionDelay = 1.0f;

    [Header("6. 스킬 및 사망 연출")]
    public GameObject skillEffectOnScene;
    public float skillDelay = 0.5f;
    public float deathDelay = 1.5f;

    [Header("7. 카메라 연출 (시네머신)")]
    public CinemachineVirtualCamera virtualCamera;
    public float targetCamSize = 3.5f;

    [Header("8. 스프라이트 수동 교체")]
    public Sprite playerIdleRight;
    public Sprite helperIdleLeft;

    [Header("9. 사운드 효과")]
    public AudioSource audioSource;      // 소리를 재생할 스피커 컴포넌트
    public AudioClip skillSound;         // 스킬 발동 효과음
    public AudioClip monsterDeathSound;  // 몬스터 사망 효과음

    private bool isCutsceneRunning = false;
    private Animator fakePlayerAnimator;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCutsceneRunning) return;
        if (other.GetComponent<PlayerAction>() != null)
        {
            if (nextCutscenePart == null) Debug.LogError("다음 컷씬 연결 안됨!");
            StartCoroutine(CutsceneCoroutine());
        }
    }

    IEnumerator CutsceneCoroutine()
    {
        isCutsceneRunning = true;
        if (GameManager.Instance != null) GameManager.Instance.isAction = true;

        fakePlayerAnimator = fakePlayerActor.GetComponent<Animator>();

        // [시작: 무조건 오른쪽 보기]
        if (fakePlayerAnimator != null)
        {
            fakePlayerAnimator.SetInteger("hAxisRaw", 1);
            fakePlayerAnimator.SetInteger("vAxisRaw", 0);
            fakePlayerAnimator.SetBool("isChange", false);
            fakePlayerAnimator.Update(0f);
        }

        player.gameObject.SetActive(false);
        fakePlayerActor.transform.position = player.transform.position;
        fakePlayerActor.transform.rotation = player.transform.rotation;
        fakePlayerActor.SetActive(true);

        // 몬스터 움직임 멈춤
        UndeadMover monsterMover = monster.GetComponent<UndeadMover>();
        if (monsterMover != null) { monsterMover.StopAllCoroutines(); monsterMover.enabled = false; }

        // ▼▼▼ [추가됨] 몬스터가 내고 있던 소리 끄기 ▼▼▼
        if (monster != null)
        {
            AudioSource monsterAudio = monster.GetComponent<AudioSource>();
            if (monsterAudio != null)
            {
                monsterAudio.Stop(); // 몬스터 소리 즉시 정지
            }
        }
        // ▲▲▲▲▲▲

        if (SceneFader.instance != null)
        {
            yield return StartCoroutine(SceneFader.instance.Fade(1f));
            if (virtualCamera != null) virtualCamera.m_Lens.OrthographicSize = targetCamSize;
            yield return StartCoroutine(SceneFader.instance.Fade(0f));
        }

        if (dialogues.Length > 0) { DialogueManager.instance.StartDialogue(dialogues[0]); yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive); }

        // --- 이동 ---
        bool playerAtTarget = false; bool monsterAtTarget = false;
        while (!playerAtTarget || !monsterAtTarget)
        {
            if (!playerAtTarget)
            {
                fakePlayerActor.transform.position = Vector3.MoveTowards(fakePlayerActor.transform.position, playerTargetPoint.position, playerMoveSpeed * Time.deltaTime);
                if (fakePlayerAnimator != null)
                {
                    Vector2 dir = (playerTargetPoint.position - fakePlayerActor.transform.position).normalized;
                    int h = 0, v = 0;
                    if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) h = (int)Mathf.Sign(dir.x); else v = (int)Mathf.Sign(dir.y);
                    if (fakePlayerAnimator.GetInteger("hAxisRaw") != h) { fakePlayerAnimator.SetBool("isChange", true); fakePlayerAnimator.SetInteger("hAxisRaw", h); fakePlayerAnimator.SetInteger("vAxisRaw", 0); }
                    else if (fakePlayerAnimator.GetInteger("vAxisRaw") != v) { fakePlayerAnimator.SetBool("isChange", true); fakePlayerAnimator.SetInteger("hAxisRaw", 0); fakePlayerAnimator.SetInteger("vAxisRaw", v); }
                    else fakePlayerAnimator.SetBool("isChange", false);
                }
                if (Vector3.Distance(fakePlayerActor.transform.position, playerTargetPoint.position) < 0.01f) playerAtTarget = true;
            }
            if (!monsterAtTarget)
            {
                monster.transform.position = Vector3.MoveTowards(monster.transform.position, monsterTargetPoint.position, monsterMoveSpeed * Time.deltaTime);
                if (Vector3.Distance(monster.transform.position, monsterTargetPoint.position) < 0.01f) monsterAtTarget = true;
            }
            yield return null;
        }

        // 이동 후 오른쪽 보기 고정
        if (fakePlayerAnimator != null) fakePlayerAnimator.enabled = false;
        SpriteRenderer fakePlayerSR = fakePlayerActor.GetComponent<SpriteRenderer>();
        if (fakePlayerSR != null && playerIdleRight != null) fakePlayerSR.sprite = playerIdleRight;

        if (dialogues.Length > 1) { DialogueManager.instance.StartDialogue(dialogues[1]); yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive); }

        // --- 조력자 이동 ---
        helper.transform.position = helperSpawnPoint.position;
        helper.SetActive(true);
        if (helperTargetPoint != null)
        {
            Animator helperAnim = helper.GetComponent<Animator>();
            SpriteRenderer helperSR = helper.GetComponent<SpriteRenderer>();
            if (helperAnim != null) helperAnim.enabled = true;
            Vector3 targetPos = helperTargetPoint.position;
            targetPos.z = helper.transform.position.z;
            while (Vector3.Distance(helper.transform.position, targetPos) > 0.01f)
            {
                helper.transform.position = Vector3.MoveTowards(helper.transform.position, targetPos, helperMoveSpeed * Time.deltaTime);
                if (helperAnim != null)
                {
                    Vector2 dir = (targetPos - helper.transform.position).normalized;
                    int h = 0; int v = 0;
                    if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) h = (int)Mathf.Sign(dir.x); else v = (int)Mathf.Sign(dir.y);
                    if (helperAnim.GetInteger("hAxisRaw") != h) { helperAnim.SetBool("isChange", true); helperAnim.SetInteger("hAxisRaw", h); helperAnim.SetInteger("vAxisRaw", 0); }
                    else if (helperAnim.GetInteger("vAxisRaw") != v) { helperAnim.SetBool("isChange", true); helperAnim.SetInteger("hAxisRaw", 0); helperAnim.SetInteger("vAxisRaw", v); }
                    else helperAnim.SetBool("isChange", false);
                }
                yield return null;
            }
            helper.transform.position = targetPos;
            if (helperAnim != null) helperAnim.enabled = false;
            if (helperSR != null && helperIdleLeft != null) { helperSR.sprite = helperIdleLeft; }
        }
        yield return new WaitForSeconds(0.5f);

        if (dialogues.Length > 2) { DialogueManager.instance.StartDialogue(dialogues[2]); yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive); }

        // 스킬 발동 전 딜레이
        if (skillDelay > 0) yield return new WaitForSeconds(skillDelay);

        // 스킬 사운드 재생
        if (audioSource != null && skillSound != null)
        {
            audioSource.PlayOneShot(skillSound);
        }

        if (skillEffectOnScene != null) skillEffectOnScene.SetActive(true);

        // 몬스터 사망 사운드 재생
        if (audioSource != null && monsterDeathSound != null)
        {
            audioSource.PlayOneShot(monsterDeathSound);
        }

        Animator monsterAnim = monster.GetComponent<Animator>();
        if (monsterAnim != null) monsterAnim.SetTrigger("doDie");

        yield return new WaitForSeconds(deathDelay);
        monster.SetActive(false);
        if (skillEffectOnScene != null) skillEffectOnScene.SetActive(false);

        if (dialogues.Length > 3)
        {
            DialogueManager.instance.StartDialogue(dialogues[3]);
            yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive);
        }

        if (transitionDelay > 0) yield return new WaitForSeconds(transitionDelay);

        fakePlayerActor.SetActive(false);
        player.transform.position = fakePlayerActor.transform.position;

        // 진짜 플레이어 오른쪽 고정으로 시작
        Animator realPlayerAnim = player.GetComponent<Animator>();
        if (realPlayerAnim != null) realPlayerAnim.enabled = false;
        SpriteRenderer realPlayerSR = player.GetComponent<SpriteRenderer>();
        if (realPlayerSR != null && playerIdleRight != null) realPlayerSR.sprite = playerIdleRight;

        player.gameObject.SetActive(true);

        if (nextCutscenePart != null) nextCutscenePart.StartPart2();
        else
        {
            if (GameManager.Instance != null) GameManager.Instance.isAction = false;
            gameObject.SetActive(false);
        }
    }
}