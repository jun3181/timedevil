using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Cinemachine;

[RequireComponent(typeof(BoxCollider2D))]
public class Cutscene_Cornered_03 : MonoBehaviour
{
    [Header("1. 컷씬 대상")]
    public PlayerAction player;
    public GameObject fakePlayerActor;
    public GameObject helper2;

    [Header("2. 위치 설정")]
    public Transform playerTargetPoint;  // 플레이어가 걸어갈 곳
    public Transform helperSpawnPoint;   // 조력자2 등장 위치
    public Transform helperTargetPoint;  // 조력자2가 걸어올 위치

    [Header("3. 설정값")]
    public float playerMoveSpeed = 2.5f;
    public float helperMoveSpeed = 2.5f;
    public string nextSceneName;

    [Header("4. 대화")]
    public Dialogue dialogueAfterSpawn;  // 등장 직후 대사
    public Dialogue dialogueAfterWalk;   // 걸어온 뒤 대사

    [Header("5. 스프라이트 수동 교체")]
    public Sprite playerIdleRight;       // 플레이어 오른쪽 보기
    public Sprite helperIdleLeft;        // 조력자 왼쪽 보기

    [Header("6. 카메라 설정")]
    // ★ 여기에 2번 컷씬처럼 카메라 오브젝트를 넣으시면 됩니다.
    public GameObject cutsceneCameraObject;

    private bool isCutsceneRunning = false;
    private Animator fakePlayerAnimator;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCutsceneRunning) return;
        if (other.GetComponent<PlayerAction>() != null)
        {
            if (player == null || fakePlayerActor == null || helper2 == null ||
                playerTargetPoint == null || helperSpawnPoint == null || helperTargetPoint == null)
            {
                Debug.LogError("[Cutscene_Cornered_03] 필수 오브젝트가 연결되지 않았습니다!");
                return;
            }
            StartCoroutine(CutsceneSequence());
        }
    }

    IEnumerator CutsceneSequence()
    {
        isCutsceneRunning = true;
        if (GameManager.Instance != null) GameManager.Instance.isAction = true;

        // 1. 배우 플레이어 교체 & 오른쪽 보기 고정
        fakePlayerAnimator = fakePlayerActor.GetComponent<Animator>();
        player.gameObject.SetActive(false);
        fakePlayerActor.transform.position = player.transform.position;
        fakePlayerActor.transform.rotation = player.transform.rotation;
        fakePlayerActor.SetActive(true);

        // 애니메이터 끄고 오른쪽 스프라이트 고정
        if (fakePlayerAnimator != null) fakePlayerAnimator.enabled = false;
        SpriteRenderer fakeSR = fakePlayerActor.GetComponent<SpriteRenderer>();
        if (fakeSR != null && playerIdleRight != null)
        {
            fakeSR.sprite = playerIdleRight;
        }

        // 2. 화면 페이드 & 카메라 전환
        if (SceneFader.instance != null)
        {
            // 화면 어둡게 (Fade In)
            yield return StartCoroutine(SceneFader.instance.Fade(1f));

            // ★ 화면이 깜깜할 때 컷씬 카메라 켜기
            if (cutsceneCameraObject != null) cutsceneCameraObject.SetActive(true);

            // 화면 밝게 (Fade Out)
            yield return StartCoroutine(SceneFader.instance.Fade(0f));
        }
        else
        {
            // 페이더가 없으면 즉시 카메라 전환
            if (cutsceneCameraObject != null) cutsceneCameraObject.SetActive(true);
        }

        // ★ 페이드 끝나자마자 딜레이 없이 플레이어 이동 시작 ★

        // 3. 플레이어 이동
        if (playerTargetPoint != null)
        {
            // 이동할 땐 애니메이터 켜기
            if (fakePlayerAnimator != null) fakePlayerAnimator.enabled = true;

            while (Vector3.Distance(fakePlayerActor.transform.position, playerTargetPoint.position) > 0.01f)
            {
                fakePlayerActor.transform.position = Vector3.MoveTowards(
                    fakePlayerActor.transform.position,
                    playerTargetPoint.position,
                    playerMoveSpeed * Time.deltaTime
                );

                if (fakePlayerAnimator != null)
                {
                    Vector2 dir = (playerTargetPoint.position - fakePlayerActor.transform.position).normalized;
                    int h = 0; int v = 0;
                    if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) h = (int)Mathf.Sign(dir.x); else v = (int)Mathf.Sign(dir.y);

                    if (fakePlayerAnimator.GetInteger("hAxisRaw") != h) { fakePlayerAnimator.SetBool("isChange", true); fakePlayerAnimator.SetInteger("hAxisRaw", h); fakePlayerAnimator.SetInteger("vAxisRaw", 0); }
                    else if (fakePlayerAnimator.GetInteger("vAxisRaw") != v) { fakePlayerAnimator.SetBool("isChange", true); fakePlayerAnimator.SetInteger("hAxisRaw", 0); fakePlayerAnimator.SetInteger("vAxisRaw", v); }
                    else fakePlayerAnimator.SetBool("isChange", false);
                }
                yield return null;
            }

            // 이동 끝: 다시 애니메이터 끄고 오른쪽 고정
            if (fakePlayerAnimator != null) fakePlayerAnimator.enabled = false;
            if (fakeSR != null && playerIdleRight != null) fakeSR.sprite = playerIdleRight;
        }

        // 4. 조력자2 등장
        helper2.transform.position = helperSpawnPoint.position;
        helper2.SetActive(true);

        // 왼쪽 보기 고정
        Animator helperAnim = helper2.GetComponent<Animator>();
        SpriteRenderer helperSR = helper2.GetComponent<SpriteRenderer>();
        if (helperAnim != null) helperAnim.enabled = false;
        if (helperSR != null && helperIdleLeft != null) helperSR.sprite = helperIdleLeft;

        // 5. 첫 번째 대화
        if (dialogueAfterSpawn.sentences.Length > 0)
        {
            DialogueManager.instance.StartDialogue(dialogueAfterSpawn);
            yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive);
        }

        // 6. 조력자2 이동
        if (helperTargetPoint != null)
        {
            if (helperAnim != null) helperAnim.enabled = true; // 이동 위해 애니메이터 켬

            Vector3 targetPos = helperTargetPoint.position;
            targetPos.z = helper2.transform.position.z;

            while (Vector3.Distance(helper2.transform.position, targetPos) > 0.01f)
            {
                helper2.transform.position = Vector3.MoveTowards(helper2.transform.position, targetPos, helperMoveSpeed * Time.deltaTime);

                if (helperAnim != null)
                {
                    Vector3 dir = (targetPos - helper2.transform.position).normalized;
                    int h = 0; int v = 0;
                    if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) h = (int)Mathf.Sign(dir.x); else v = (int)Mathf.Sign(dir.y);

                    if (helperAnim.GetInteger("hAxisRaw") != h) { helperAnim.SetBool("isChange", true); helperAnim.SetInteger("hAxisRaw", h); helperAnim.SetInteger("vAxisRaw", 0); }
                    else if (helperAnim.GetInteger("vAxisRaw") != v) { helperAnim.SetBool("isChange", true); helperAnim.SetInteger("hAxisRaw", 0); helperAnim.SetInteger("vAxisRaw", v); }
                    else helperAnim.SetBool("isChange", false);
                }
                yield return null;
            }
            helper2.transform.position = targetPos;

            // 이동 끝: 왼쪽 보기 고정
            if (helperAnim != null) helperAnim.enabled = false;
            if (helperSR != null && helperIdleLeft != null) helperSR.sprite = helperIdleLeft;
        }

        // 7. 두 번째 대화
        if (dialogueAfterWalk.sentences.Length > 0)
        {
            DialogueManager.instance.StartDialogue(dialogueAfterWalk);
            yield return new WaitUntil(() => !DialogueManager.instance.isDialogueActive);
        }

        // 8. 씬 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneFader.instance != null)
            {
                yield return StartCoroutine(SceneFader.instance.Fade(1f));
            }
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // 복구 (테스트용)
            if (cutsceneCameraObject != null) cutsceneCameraObject.SetActive(false);
            fakePlayerActor.SetActive(false);
            player.transform.position = fakePlayerActor.transform.position;
            player.gameObject.SetActive(true);
            if (GameManager.Instance != null) GameManager.Instance.isAction = false;
            gameObject.SetActive(false);
        }
    }
}