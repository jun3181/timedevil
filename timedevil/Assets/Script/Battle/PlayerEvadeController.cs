//using System.Collections;
using UnityEngine;

public class PlayerEvadeController : MonoBehaviour
{
    /*
    [Header("Target Pawn (���� �̵��� Transform)")]
    [SerializeField] private Transform playerPawn;

    [Header("Animator")]
    [SerializeField] private PlayerAnimeController animator;

    [Header("Grid Step (�� ĭ ũ��)")]
    [SerializeField] private float stepX = 1.3f;
    [SerializeField] private float stepY = 1.3f;

    [Header("Allowed Tile Centers (Panel ���)")]
    [Tooltip("�÷��̾� ������ 16�� ����(���� ��ǥ). GridOrigin/AttackController���� ���� �Ͱ� �����ϰ� ����")]
    [SerializeField] private Vector3[] allowedCenters = new Vector3[16];
    [SerializeField, Tooltip("���� ������ ���Ϳ� �󸶳� ������� �������(���� �Ÿ�)")]
    private float snapEpsilon = 0.15f;

    [Header("Timing")]
    [SerializeField, Tooltip("���� �̵� �ð�(��)")]
    private float moveSeconds = 0.25f;

    private bool enemyAttackWindow = false;
    private bool evading = false; // �̵� �� �Է� ���

    void Awake()
    {
        if (!playerPawn) playerPawn = this.transform;
        if (!animator) animator = FindObjectOfType<PlayerAnimeController>(true);
        if (animator) animator.SetTarget(playerPawn);
    }

    void OnEnable() { EnemyTurnController.OnEnemyAttackWindowChanged += HandleEnemyAttackWindow; }
    void OnDisable() { EnemyTurnController.OnEnemyAttackWindowChanged -= HandleEnemyAttackWindow; }

    private void HandleEnemyAttackWindow(bool on) { enemyAttackWindow = on; }

    void Update()
    {
        if (!CanAcceptInput()) return;

        // ���� �Է�
        Vector3 offset = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow)) offset = new Vector3(0f, stepY, 0f);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) offset = new Vector3(0f, -stepY, 0f);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) offset = new Vector3(-stepX, 0f, 0f);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) offset = new Vector3(stepX, 0f, 0f);
        else return;

        // ���� �ĺ� �� �г� �������� ���� �˻�
        var cur = playerPawn.position;
        if (!TrySnapToAllowedCenter(cur + offset, out var snappedEnd))
            return; // �г� ���̸� ����

        StartCoroutine(Co_MoveOnce(snappedEnd));
    }

    private bool CanAcceptInput()
    {
        if (evading) return false;
        var tm = TurnManager.Instance;
        if (tm == null || tm.currentTurn != TurnState.EnemyTurn) return false; // �� �Ͽ��� ȸ��
        if (!enemyAttackWindow) return false;                                  // ���� ������ �߿��� ȸ��
        return true;
    }

    /// <summary>
    /// �� ĭ�� �̵��ϰ� ��(����ġ ���� ����)
    /// </summary>
    private IEnumerator Co_MoveOnce(Vector3 endWorld)
    {
        evading = true;

        float dur = Mathf.Max(0.01f, moveSeconds);

        if (animator != null)
        {
            animator.AnimateTo(endWorld, dur);
            while (animator.IsPlaying) yield return null;
        }
        else
        {
            yield return LerpPosition(playerPawn.position, endWorld, dur);
        }

        // ���� ����(�ε����� ����)
        playerPawn.position = endWorld;

        evading = false;
    }

    // --- Allowed center check ---
    private bool TrySnapToAllowedCenter(Vector3 desired, out Vector3 snapped)
    {
        snapped = desired;
        if (allowedCenters == null || allowedCenters.Length == 0) return true; // ��� �̼��� �� ���

        // ���� ����� ���� ã��
        float best = float.MaxValue;
        int bestIdx = -1;
        for (int i = 0; i < allowedCenters.Length; i++)
        {
            float d = Vector2.Distance((Vector2)desired, (Vector2)allowedCenters[i]);
            if (d < best) { best = d; bestIdx = i; }
        }

        if (bestIdx >= 0 && best <= snapEpsilon)
        {
            snapped = allowedCenters[bestIdx];
            return true; // �г� ���η� ����
        }
        return false;     // �г� Ż�� �� �̵� ����
    }

    // ���� ����(�ִϸ����� ���� ��)
    private IEnumerator LerpPosition(Vector3 a, Vector3 b, float dur)
    {
        float t = 0f;
        dur = Mathf.Max(0.01f, dur);
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            playerPawn.position = Vector3.Lerp(a, b, u);
            yield return null;
        }
        playerPawn.position = b;
    }

*/
}


