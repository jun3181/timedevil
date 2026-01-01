using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KeySfxPlayer : MonoBehaviour
{
    [Header("효과음 재생용 오디오 소스")]
    [SerializeField] private AudioSource audioSource;

    [Header("효과음 클립들")]
    [SerializeField] private AudioClip arrowClip;   // ↑↓←→ 공통
    [SerializeField] private AudioClip fClip;       // F 키
    [SerializeField] private AudioClip dClip;       // D 키

    [Header("재생 길이 (초)")]
    [SerializeField] private float arrowDuration = 0.07f;
    [SerializeField] private float fDuration = 0.10f;
    [SerializeField] private float dDuration = 0.10f;

    // 이 이름이 현재 씬 이름이랑 같을 때만 동작
    private const string InventorySceneName = "InventoryScene";
    private bool isInventoryScene = false;

    private void Awake()
    {
        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        isInventoryScene = SceneManager.GetActiveScene().name == InventorySceneName;
    }

    private void Update()
    {
        // 인벤토리 씬이 아니면 아예 입력 안 받음
        if (!isInventoryScene) return;

        // ↑ ↓ ← → : 같은 소리 짧게
        if (Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            PlayShort(arrowClip, arrowDuration);
        }

        // F 키
        if (Input.GetKeyDown(KeyCode.F))
        {
            PlayShort(fClip, fDuration);
        }

        // D 키
        if (Input.GetKeyDown(KeyCode.D))
        {
            PlayShort(dClip, dDuration);
        }
    }

    private void PlayShort(AudioClip clip, float duration)
    {
        if (audioSource == null || clip == null) return;

        // 이전 재생 코루틴 있으면 끊고 새로 시작
        StopAllCoroutines();
        StartCoroutine(PlayShortRoutine(clip, duration));
    }

    private IEnumerator PlayShortRoutine(AudioClip clip, float duration)
    {
        audioSource.clip = clip;
        audioSource.time = 0f;
        audioSource.Play();
        yield return new WaitForSeconds(duration);
        audioSource.Stop();
    }
}
