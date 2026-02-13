// Assets/Script/Mainmenu/GameStartContext.cs
public enum GameStartMode
{
    NewGame,
    LoadGame
}

public static class GameStartContext
{
    // 이번 실행에서만 유지(저장 X)
    public static GameStartMode Mode { get; private set; } = GameStartMode.NewGame;

    // "새게임/이어하기 버튼을 누른 횟수" 토큰 (씬 재진입 중복 실행 방지용)
    public static int StartToken { get; private set; } = 0;

    public static void SetNewGame()
    {
        Mode = GameStartMode.NewGame;
        StartToken++;
    }

    public static void SetLoadGame()
    {
        Mode = GameStartMode.LoadGame;
        StartToken++;
    }

    public static void ResetToDefault()
    {
        Mode = GameStartMode.NewGame;
        // 토큰은 굳이 리셋 안 함(에디터 테스트용)
    }
}
