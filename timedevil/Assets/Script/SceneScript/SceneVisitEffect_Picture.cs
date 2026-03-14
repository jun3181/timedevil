// Assets/Script/Scene/VisitEffect/SceneVisitEffect_Picture.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class SceneVisitEffect_Picture : SceneVisitEffectBase
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image image;

    [Header("Enter Frames (Scene Start)")]
    [SerializeField] private List<Sprite> enterFrames = new();
    [Min(0f)][SerializeField] private float enterTickSeconds = 0.12f;
    [SerializeField] private bool hideAfterEnter = true;

    [Header("Exit Frames (Before Load Next Scene)")]
    [SerializeField] private List<Sprite> exitFrames = new();
    [Min(0f)][SerializeField] private float exitTickSeconds = 0.12f;
    [SerializeField] private bool useEnterFramesIfExitEmpty = true;
    [SerializeField] private bool holdLastFrameOnExit = true;

    [Header("Input Block")]
    [SerializeField] private bool blockRaycastsWhileVisible = true;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
    }

    private void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (!image) image = GetComponent<Image>();

        // 기본: 안 보이는 상태로 시작
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;

        image.enabled = true;                 //  스프라이트 교체하려면 켜져있어야 안전
        var c = image.color; c.a = 1f; image.color = c; //  알파 1 강제
        image.raycastTarget = false;
    }

    public override IEnumerator PlayEnter()
    {
        if (enterFrames == null || enterFrames.Count == 0)
        {
            Hide();
            yield break;
        }

        Show();

        for (int i = 0; i < enterFrames.Count; i++)
        {
            image.sprite = enterFrames[i];
            yield return WaitTick(enterTickSeconds);
        }

        if (hideAfterEnter) Hide();
        else Show(); // 마지막 프레임 유지
    }

    public override IEnumerator PlayExit()
    {
        var frames = exitFrames;
        if ((frames == null || frames.Count == 0) && useEnterFramesIfExitEmpty)
            frames = enterFrames;

        if (frames == null || frames.Count == 0)
        {
            // 프레임이 아예 없으면 그냥 검정도 띄우지 않음
            Hide();
            yield break;
        }

        Show();

        for (int i = 0; i < frames.Count; i++)
        {
            image.sprite = frames[i];
            yield return WaitTick(exitTickSeconds);
        }

        if (!holdLastFrameOnExit) Hide();
        else Show(); // 마지막 프레임 유지 (씬 로드 직전까지)
    }

    private void Show()
    {
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = blockRaycastsWhileVisible;

        image.enabled = true;
        var c = image.color; c.a = 1f; image.color = c;
    }

    private void Hide()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private IEnumerator WaitTick(float sec)
    {
        if (sec <= 0f) { yield return null; yield break; }

        float t = 0f;
        while (t < sec)
        {
            t += DeltaTime;
            yield return null;
        }
    }
}
