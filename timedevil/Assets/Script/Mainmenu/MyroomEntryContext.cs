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

    //  Entry Ǿ  Һϰ true ȯ
    public static bool TryConsume(out MyroomEntryPoint entry)
        entry = _next;
        if (entry == MyroomEntryPoint.None)
            return false;

        Current = entry;
        return true;
    }

    //    fallback (ȣȯ)
    public static MyroomEntryPoint Consume(MyroomEntryPoint fallback)
    {
        Current = TryConsume(out var v) ? v : fallback;
    public static void SetRoom2() => _next = MyroomEntryPoint.Room2;

    // 한 번 쓰면 자동 초기화(원샷)
    public static MyroomEntryPoint Consume(MyroomEntryPoint fallback)
    {
        var v = _next;
        _next = MyroomEntryPoint.None;

        Current = (v == MyroomEntryPoint.None) ? fallback : v;
        return Current;
    }

    // 에디터 테스트용(선택)
    public static void Clear()
    {
        _next = MyroomEntryPoint.None;
        Current = MyroomEntryPoint.None;
    }
}
