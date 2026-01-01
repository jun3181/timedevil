using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public AudioSource sfxPlayer;   // 인스펙터에서 연결
    public AudioClip clickSound;    // 인스펙터에서 연결

    public void LoadMyRoom()
    {
        if (sfxPlayer != null && clickSound != null)
            sfxPlayer.PlayOneShot(clickSound);

        if (SceneFader.instance != null)
        {
            SceneFader.instance.LoadSceneWithFade("Myroom");
        }
        else
        {
            Debug.LogError("SceneFader.instance가 null입니다! SceneFader 오브젝트가 씬에 없거나 비활성화된 것 같습니다.");
        }
    }
}
