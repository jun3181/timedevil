// Assets/Script/Scene/MyroomEntryContext.cs
public enum MyroomEntryPoint
{
    None,
    Room1,
    Room2,
    Room3
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

    public static void SetRoom1()
    {
        _next = MyroomEntryPoint.Room1;
    }

    public static void SetRoom2()
    {
        _next = MyroomEntryPoint.Room2;
    }

    public static void SetRoom3()
    {
        _next = MyroomEntryPoint.Room3;
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
