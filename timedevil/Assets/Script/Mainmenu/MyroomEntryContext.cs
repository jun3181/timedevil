// Assets/Script/Scene/MyroomEntryContext.cs
public enum MyroomEntryPoint
{
    None = 0,
    Spawn_Room1_NewGame = 1,
    Spawn_Room2_LoadGame_PlayerDead = 2,
    Spawn_Room4_end = 4
}

public static class MyroomEntryContext
{
    private static MyroomEntryPoint _next = MyroomEntryPoint.None;

    // Last applied entry for debug/reference.
    public static MyroomEntryPoint Current { get; private set; }

    static MyroomEntryContext()
    {
        Current = MyroomEntryPoint.None;
    }

    public static void SetNewGame()
    {
        _next = MyroomEntryPoint.Spawn_Room1_NewGame;
    }

    public static void SetLoadGamePlayerDead()
    {
        _next = MyroomEntryPoint.Spawn_Room2_LoadGame_PlayerDead;
    }

    public static void SetEnd()
    {
        _next = MyroomEntryPoint.Spawn_Room4_end;
    }

    // Consume only when an explicit entry exists.
    public static bool TryConsume(out MyroomEntryPoint entry)
    {
        entry = _next;
        _next = MyroomEntryPoint.None;

        if (entry == MyroomEntryPoint.None)
            return false;

        Current = entry;
        return true;
    }

    // Compatibility helper.
    public static MyroomEntryPoint Consume(MyroomEntryPoint fallback)
    {
        MyroomEntryPoint consumed;
        if (TryConsume(out consumed))
            Current = consumed;
        else
            Current = fallback;

        return Current;
    }

    public static void Clear()
    {
        _next = MyroomEntryPoint.None;
        Current = MyroomEntryPoint.None;
    }
}
