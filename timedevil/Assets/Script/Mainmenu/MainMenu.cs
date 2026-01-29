// Assets/Script/Mainmenu/MainMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioSource sfxPlayer;
    public AudioClip clickSound;

    [Header("Target Scene")]
    public string myRoomSceneName = "Myroom";

    public void LoadMyRoom()
    {
        if (sfxPlayer != null && clickSound != null)
            sfxPlayer.PlayOneShot(clickSound);

        var fader = FindObjectOfType<SceneFader>(true);
        if (fader != null)
        {
            fader.LoadSceneWithFadeOut(myRoomSceneName);
        }
        else
        {
            Debug.LogWarning("[MainMenu] SceneFader를 찾지 못했습니다. 즉시 LoadScene 합니다.");
            SceneManager.LoadScene(myRoomSceneName);
        }
    }
}
