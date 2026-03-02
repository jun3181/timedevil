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

        // ✅ 저장 유무와 무관하게 "완전 새 시작" 보장
        SaveSystem.ClearAllSaves();

        // ✅ 1회성 진입점 지정
        MyroomEntryContext.SetRoom1();

        // (유지) 기존 컨텍스트도 그대로
        GameStartContext.SetNewGame();

        LoadMyRoom();
    }

    // 버튼: 이어하기 (Room3 스폰으로 진입)
    public void LoadGame()
    {
        PlayClick();

        // ✅ 1회성 진입점 지정 (Load는 Room3 스폰 사용)
        MyroomEntryContext.SetRoom3();

        // (유지) 기존 컨텍스트도 그대로
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
