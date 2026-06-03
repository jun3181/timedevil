// DrawCardSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum DrawMode { UpDraw, AntiDraw, HandRefresh, HandEffectSequence }
public enum DrawHandEffectType { SelfDiscard, SelfDraw, OpponentDiscard, OpponentDraw }

[Serializable]
public class DrawHandEffectStep
{
    public DrawHandEffectType effectType = DrawHandEffectType.SelfDraw;
    [Min(0)] public int amount = 1;
}

[CreateAssetMenu(menuName = "Cards/Draw Card", fileName = "DrawCard")]
public class DrawCardSO : BaseCardSO
{
    public DrawMode drawMode = DrawMode.UpDraw;
    public int amount = 1;      // UpDraw: 자신 드로우 / AntiDraw: 상대 손패 버리기

    [Header("HandRefresh")]
    public int refreshDiscardAmount = 1;
    public int refreshDrawAmount = 2;

    [Header("Ordered Hand Effects")]
    [Tooltip("HandEffectSequence 모드에서 위에서 아래 순서대로 처리됩니다.")]
    public List<DrawHandEffectStep> handEffectSequence = new List<DrawHandEffectStep>
    {
        new DrawHandEffectStep { effectType = DrawHandEffectType.SelfDraw, amount = 1 }
    };
}
