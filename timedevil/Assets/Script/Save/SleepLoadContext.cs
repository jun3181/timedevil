public static class SleepLoadContext
{
    private static bool _pending = false;

    // 침대에서 "로드로 씬 이동" 직전에 호출
    public static void MarkPending() => _pending = true;

    // 씬 들어온 쪽(Applier)에서 1회 소비
    public static bool Consume()
    {
        bool v = _pending;
        _pending = false;
        return v;
    }
}