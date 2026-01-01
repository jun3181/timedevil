public interface ICardPattern
{
    string CardImagePath { get; }
    string Pattern16 { get; }
    float[] Timings { get; } // 인덱스별 발동 시간
}
