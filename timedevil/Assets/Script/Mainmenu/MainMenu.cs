// Assets/Script/Mainmenu/MainMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioSource sfxPlayer;
    public AudioClip clickSound;

    [Header("Target Scene")]
    public string myRoomSceneName = "Myroom";

    // 버튼: 새 게임
    public void NewGame()
    {
        PlayClick();
        GameStartContext.SetNewGame();
        LoadMyRoom();
    }

    // 버튼: 이어하기(지금은 '2번방'으로 가는 분기만)
    public void LoadGame()
    {
        PlayClick();
        GameStartContext.SetLoadGame();
        LoadMyRoom();
    }

    private void LoadMyRoom()
    {
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

    private void PlayClick()
    {
        if (sfxPlayer != null && clickSound != null)
            sfxPlayer.PlayOneShot(clickSound);
    }
}
