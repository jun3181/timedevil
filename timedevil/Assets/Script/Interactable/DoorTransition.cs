using UnityEngine;
using System.Collections;
using Cinemachine;

public class DoorTransition : MonoBehaviour, IInteractable
{
    [Header("이동할 목표 지점")]
    public Transform targetPoint;

    [Header("카메라 설정")]
    public CinemachineVirtualCamera virtualCamera;
    public float newCameraSize = 8f;

    [Header("BGM 설정")]
    public AudioClip newBGM;

    [Header("음악 반복 방지")]
    public string stopMusicKey = "Chapter1_Intro_BGM";

    // ▼▼▼ [추가됨] 문 여는 효과음 설정 ▼▼▼
    [Header("효과음 설정")]
    public AudioSource sfxAudioSource; // 소리를 낼 스피커
    public AudioClip doorOpenSound;    // 문 여는 소리 파일
    // ▲▲▲▲▲▲

    private PlayerAction player;
    private bool isTransitioning = false;

    void Start()
    {
        player = FindObjectOfType<PlayerAction>();
        if (player == null)
        {
            Debug.LogError("[DoorTransition] 씬에서 'PlayerAction' 스크립트를 찾을 수 없습니다!");
        }
    }

    public void Interact()
    {
        if (targetPoint == null) return;
        if (player == null) return;

        if (isTransitioning || (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive))
        {
            return;
        }
        StartCoroutine(TransitionCoroutine());
    }

    IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;

        // 1. 플레이어 조작 비활성화
        if (GameManager.Instance != null) GameManager.Instance.isAction = true;

        // ▼▼▼ [추가됨] 문 여는 소리 재생 ▼▼▼
        // 화면이 어두워지기 시작할 때 소리가 나야 자연스럽습니다.
        if (sfxAudioSource != null && doorOpenSound != null)
        {
            sfxAudioSource.PlayOneShot(doorOpenSound);
        }
        // ▲▲▲▲▲▲

        // 2. 페이드 아웃
        if (SceneFader.instance != null) yield return StartCoroutine(SceneFader.instance.Fade(1f));

        // --- 화면이 검은 상태 ---

        // 3. BGM 변경
        if (newBGM != null && BGMManager.instance != null)
        {
            BGMManager.instance.PlayBGM(newBGM);
        }

        // 4. 음악 반복 방지 저장
        if (!string.IsNullOrEmpty(stopMusicKey))
        {
            PlayerPrefs.SetInt(stopMusicKey, 1);
            PlayerPrefs.Save();
        }

        // 5. 플레이어 이동
        player.transform.position = targetPoint.position;

        // 6. 카메라 설정
        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.OrthographicSize = newCameraSize;
            virtualCamera.Follow = player.transform;
            CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null) confiner.enabled = true;
        }

        yield return null;

        // 7. 페이드 인
        if (SceneFader.instance != null) yield return StartCoroutine(SceneFader.instance.Fade(0f));

        // 8. 조작 재개
        if (GameManager.Instance != null) GameManager.Instance.isAction = false;
        isTransitioning = false;
    }
}