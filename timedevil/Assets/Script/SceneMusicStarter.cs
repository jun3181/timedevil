using UnityEngine;

public class SceneMusicStarter : MonoBehaviour
{
    [Header("이 씬에서 재생할 배경음악")]
    public AudioClip sceneBGM;

    [Header("저장 키 설정 (중복 방지용)")]
    [Tooltip("이 이름으로 저장된 기록이 있으면 음악을 틀지 않습니다.")]
    public string uniqueKey = "Chapter1_Intro_BGM";

    void Start()
    {
        // 1. 만약 "Chapter1_Intro_BGM"이라는 기록이 '1'(True)로 저장되어 있다면?
        if (PlayerPrefs.GetInt(uniqueKey, 0) == 1)
        {
            // 이미 문을 이용했으므로, 이 스크립트는 할 일을 다 했습니다.
            // 음악을 틀지 않고 스스로 삭제됩니다.
            Destroy(gameObject);
            return;
        }

        // 2. 기록이 없다면 음악 재생 (처음 방문했을 때)
        if (BGMManager.instance != null && sceneBGM != null)
        {
            BGMManager.instance.PlayBGM(sceneBGM);
        }
    }
}