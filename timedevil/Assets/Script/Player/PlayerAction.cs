using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;

public class PlayerAction : MonoBehaviour
{
    // ===== Public =====
    [Header("플레이어 설정")]
    public float Speed;

    [Header("매니저 연결")]
    public GameManager manager;
    public NextScene Next_Scene;
    public GetManager get_manager;

    // ===== Private =====
    Rigidbody2D rigid;
    Animator anim;

    float h;
    float v;
    bool isHorizonMove;

    Vector3 dirVec;
    GameObject scanObject;

    // ===== Unity =====
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // 매 프레임 호출
    void Update()
    {
        // DialogueManager가 없으면 에러가 날 수 있으므로, instance가 null이 아닐 때만 isDialogueActive를 확인
        bool isTalking = (DialogueManager.instance != null) && DialogueManager.instance.isDialogueActive;

        // 입력 (isAction 또는 isTalking 둘 중 하나라도 true이면 모든 입력을 막음)
        h = (manager.isAction || isTalking) ? 0 : Input.GetAxisRaw("Horizontal"); // 수평
        v = (manager.isAction || isTalking) ? 0 : Input.GetAxisRaw("Vertical");   // 수직

        // 애니메이션 및 방향 전환에 사용되는 키 눌림/뗌 입력도 완벽하게 차단
        bool hDown = (manager.isAction || isTalking) ? false : Input.GetButtonDown("Horizontal");
        bool vDown = (manager.isAction || isTalking) ? false : Input.GetButtonDown("Vertical");
        bool hUp = (manager.isAction || isTalking) ? false : Input.GetButtonUp("Horizontal");
        bool vUp = (manager.isAction || isTalking) ? false : Input.GetButtonUp("Vertical");

        // 애니메이션 전환 기준 (가로/세로 우선)
        if (hDown)
        {
            isHorizonMove = true;
        }
        else if (vDown)
        {
            isHorizonMove = false;
        }
        else if (hUp || vUp)
        {
            isHorizonMove = h != 0;
        }

        if (anim.GetInteger("hAxisRaw") != h)
        {
            anim.SetBool("isChange", true);
            anim.SetInteger("hAxisRaw", (int)h);
        }
        else if (anim.GetInteger("vAxisRaw") != v)
        {
            anim.SetBool("isChange", true);
            anim.SetInteger("vAxisRaw", (int)v);
        }
        else
        {
            anim.SetBool("isChange", false);
        }

        // 바라보는 방향 갱신 (키를 누르고 있는 동안에도 계속 마지막 방향을 기억하도록 수정)
        if (hDown || (h != 0 && isHorizonMove))
        {
            dirVec = h > 0 ? Vector3.right : Vector3.left;
        }
        else if (vDown || (v != 0 && !isHorizonMove))
        {
            dirVec = v > 0 ? Vector3.up : Vector3.down;
        }

        // --- Raycast 로직 시작 ---

        // 방향별 Raycast 거리를 다르게 설정
        float normalRayDistance = 0.01f;
        float downRayDistance = 0.25f;
        float currentRayDistance;

        if (dirVec == Vector3.down)
        {
            currentRayDistance = downRayDistance;
        }
        else
        {
            currentRayDistance = normalRayDistance;
        }

        // Raycast가 플레이어 몸 안에서 시작되는 것을 막기 위한 시작점 오프셋
        float raycastOffset = 0.5f;
        Vector2 rayOrigin = (Vector2)transform.position + ((Vector2)dirVec * raycastOffset);

        // 디버깅용 Ray 그리기
        Debug.DrawRay(rayOrigin, dirVec * currentRayDistance, new Color(0, 1, 0));

        // 실제 Raycast 발사
        RaycastHit2D rayhit = Physics2D.Raycast(
            rayOrigin,
            dirVec,
            currentRayDistance,
            LayerMask.GetMask("Object", "teleport", "item_get")
        );

        if (rayhit.collider != null)
        {
            scanObject = rayhit.collider.gameObject;
        }
        else
        {
            scanObject = null;
        }

        // --- Raycast 로직 끝 ---

        // 상호작용
        if (Input.GetKeyDown(KeyCode.E) && scanObject != null)
        {
            // 대화가 진행 중이면 다른 상호작용을 막음
            if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            {
                return;
            }

            // scanObject에서 'IInteractable' 인터페이스를 가진 컴포넌트를 찾음
            IInteractable interactable = scanObject.GetComponent<IInteractable>();

            if (interactable != null) // 인터페이스를 가진 오브젝트라면 (대화, 컷신 등)
            {
                // 상대방이 누구든 상관없이 Interact() 함수를 호출
                interactable.Interact();
            }
            else // 인터페이스가 없는 특별한 오브젝트라면 (기존 로직)
            {
                if (scanObject.layer == LayerMask.NameToLayer("teleport"))
                {
                    if (Next_Scene != null)
                        Next_Scene.LoadBattleScene(scanObject);
                }
                else if (scanObject.layer == LayerMask.NameToLayer("item_get"))
                {
                    if (get_manager != null)
                        get_manager.Action(scanObject);
                }
                else
                {
                    if (manager != null)
                        manager.Action(scanObject);
                }
            }
        }
    }

    // 물리 업데이트
    void FixedUpdate()
    {
        // 이동 (물리 효과는 FixedUpdate에서 처리)
        rigid.velocity = new Vector2(h, v).normalized * Speed;
    }
}