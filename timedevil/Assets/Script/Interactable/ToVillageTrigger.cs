using UnityEngine;
using System.Collections;
using Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class ToVillageTrigger : MonoBehaviour
{
    [Header("이동할 목표 지점")]
    public Transform targetPoint;

    [Header("카메라 설정")]
    public CinemachineVirtualCamera virtualCamera;
    public float newCameraSize = 8f;

    private PlayerAction player;
    private bool isTransitioning = false;

    void Start()
    {
        player = FindObjectOfType<PlayerAction>();
        if (player == null)
            Debug.LogError("[DoorTransition] PlayerAction을 찾을 수 없음!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        PlayerAction enteredPlayer = other.GetComponent<PlayerAction>();
        if (enteredPlayer != null)
        {
            StartCoroutine(TransitionCoroutine(enteredPlayer));
        }
    }

    IEnumerator TransitionCoroutine(PlayerAction targetPlayer)
    {
        isTransitioning = true;

        if (GameManager.Instance != null)
            GameManager.Instance.isAction = true;

        // 페이드 아웃
        yield return StartCoroutine(SceneFader.instance.Fade(1f));

        // 플레이어 위치 이동
        targetPlayer.transform.position = targetPoint.position;

        // 카메라 설정
        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.OrthographicSize = newCameraSize;
            virtualCamera.Follow = targetPlayer.transform;

            // 컨피너 2D 비활성화
            var confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null) confiner.enabled = false;
        }

        yield return null;

        // 페이드 인
        yield return StartCoroutine(SceneFader.instance.Fade(0f));

        if (GameManager.Instance != null)
            GameManager.Instance.isAction = false;

        isTransitioning = false;
    }
}
