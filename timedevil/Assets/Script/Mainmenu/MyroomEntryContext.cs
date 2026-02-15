// Assets/Script/Scene/MyroomEntryContext.cs
public enum MyroomEntryPoint
{
    None,
    Room1,
    Room2
}

public static class MyroomEntryContext
{
    private static MyroomEntryPoint _next = MyroomEntryPoint.None;

    public static void SetRoom1() => _next = MyroomEntryPoint.Room1;
    public static void SetRoom2() => _next = MyroomEntryPoint.Room2;

    // 한 번 쓰면 자동 초기화(원샷)
    public static MyroomEntryPoint Consume(MyroomEntryPoint fallback)
    {
        var v = _next;
        _next = MyroomEntryPoint.None;
        return (v == MyroomEntryPoint.None) ? fallback : v;
    }
}
