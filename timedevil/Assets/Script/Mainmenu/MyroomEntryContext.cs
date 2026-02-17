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

    // (선택) 이번 Myroom 진입에서 실제로 사용된 Entry 기록(디버그/다른 스크립트 참조용)
    public static MyroomEntryPoint Current { get; private set; } = MyroomEntryPoint.None;

    public static void SetRoom1() => _next = MyroomEntryPoint.Room1;
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
