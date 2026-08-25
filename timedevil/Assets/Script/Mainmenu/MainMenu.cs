// Assets/Script/Mainmenu/MainMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioSource sfxPlayer;
    public AudioClip clickSound;

    [Header("Target Scene")]
    public string myRoomSceneName = "Myroom";

    [Header("New Game Intro")]
    [SerializeField] private MainMenuNewGameIntro newGameIntro;

    private bool _newGameStarting = false;

    private void Awake()
    {
        if (newGameIntro == null)
            newGameIntro = GetComponent<MainMenuNewGameIntro>();
    }

    // 버튼: 새 게임
    public void NewGame()
    {
        if (_newGameStarting)
            return;

        PlayClick();

        if (newGameIntro != null && newGameIntro.HasPlayableIntro)
        {
            _newGameStarting = true;
            newGameIntro.Play(CompleteNewGame);
            return;
        }

        CompleteNewGame();
    }

    private void CompleteNewGame()
    {
        // 저장 유무와 무관하게 "완전 새 시작" 보장
        SaveSystem.ClearAllSaves();
        if (PlayerDataRuntime.Instance != null)
            PlayerDataRuntime.Instance.ResetToDefaults();

        // (유지) 기존 컨텍스트도 그대로
        GameStartContext.SetNewGame();

        LoadMyRoom(MyroomEntryPoint.Spawn_Room1_NewGame);
    }

    // 버튼: 이어하기
    public void LoadGame()
    {
        if (_newGameStarting)
            return;

        PlayClick();

        // (유지) 기존 컨텍스트도 그대로
        GameStartContext.SetLoadGame();
        if (PlayerDataRuntime.Instance != null)
            PlayerDataRuntime.Instance.LoadFromDisk();

        LoadMyRoom(MyroomEntryPoint.Spawn_Room2_LoadGame_PlayerDead);
    }

    private void LoadMyRoom(MyroomEntryPoint entryPoint)
    {
        SceneTransitionService.EnterMyroom(entryPoint, myRoomSceneName, useFaderIfExists: true);
    }

    private void PlayClick()
    {
        if (sfxPlayer != null && clickSound != null)
            sfxPlayer.PlayOneShot(clickSound);
    }
}
