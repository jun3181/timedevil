using UnityEngine;

public enum PortraitFocus
{
    None,
    Left,
    Right
}

[System.Serializable]
public struct DialogueLine
{
    [TextArea(2, 4)]
    public string text;

    [Tooltip("비우면 Dialogue.name 사용")]
    public string speakerName;

    [Header("이 줄에서만 바꾸고 싶을 때(비우면 이전/기본 유지)")]
    public Sprite leftPortrait;
    public Sprite rightPortrait;

    [Tooltip("누가 말하는지(= 반대쪽을 어둡게)")]
    public PortraitFocus focus;
}

[System.Serializable]
public class Dialogue
{
    [Header("Legacy")]
    public string name;
    [TextArea(2, 4)]
    public string[] sentences;

    [Header("Portraits (기본 2슬롯)")]
    public Sprite leftPortrait;
    public Sprite rightPortrait;

    [Header("Lines (이게 있으면 sentences 대신 이걸 사용)")]
    public DialogueLine[] lines;
}
