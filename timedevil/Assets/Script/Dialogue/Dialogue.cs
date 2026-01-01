using UnityEngine;

// 대화 한 줄에 필요한 정보들을 담는 클래스
[System.Serializable] // 이 어트리뷰트를 추가해야 Inspector 창에서 보입니다.
public class Sentence
{
    public string characterName; // 캐릭터 이름

    [TextArea(3, 10)]
    public string text; // 대사 내용

    public Sprite characterPortrait; // 캐릭터 초상화 (표정)

    public AudioClip voiceClip; // 성우 목소리 오디오 클립
}

// 대화 이벤트 하나에 포함될 Sentence들의 배열을 담는 클래스
[System.Serializable]
public class Dialogue
{
    public Sentence[] sentences;
}