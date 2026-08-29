using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CardTemplateView : MonoBehaviour
{
    [Header("Card Frame Sprites")]
    [SerializeField] private Sprite attackFrameSprite;
    [SerializeField] private Sprite moveDrawFrameSprite;
    [SerializeField] private Sprite supportFrameSprite;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite attackPatternIconSprite;
    [SerializeField] private Sprite trapPatternIconSprite;
    [SerializeField] private Sprite drawIconSprite;
    [SerializeField] private Sprite drawSecondaryIconSprite;
    [SerializeField] private Sprite moveIconSprite;

    [Header("Artwork")]
    [SerializeField] private Sprite testArtworkSprite;
    [SerializeField] private bool useCardMainArtwork = true;
    [SerializeField] private string cardArtworkResourcesFolder = "my_asset/CardArt";
    [SerializeField] private Rect artworkRect = new Rect(0.10f, 0.345f, 0.80f, 0.46f);
    [SerializeField, Min(0.1f)] private float artworkFillScale = 1.12f;

    [Header("Text Slots")]
    [FormerlySerializedAs("costText")]
    [SerializeField] private TMP_Text costText;
    [FormerlySerializedAs("nameText")]
    [SerializeField] private TMP_Text nameText;
    [FormerlySerializedAs("effectText")]
    [SerializeField] private TMP_Text effectText;

    [Header("Text Layout")]
    [SerializeField] private Color textColor = new Color(0.84f, 0.91f, 1f, 1f);
    [SerializeField] private Rect costTextRect = new Rect(0.065f, 0.85f, 0.17f, 0.107f);
    [SerializeField] private Rect nameTextRect = new Rect(0.35f, 0.867f, 0.60f, 0.078f);
    [SerializeField] private Rect effectTextRect = new Rect(0.56f, 0.055f, 0.35f, 0.225f);
    [SerializeField] private Rect supportEffectTextRect = new Rect(0.14f, 0.055f, 0.72f, 0.225f);
    [SerializeField, Min(1f)] private float cornerFontSize = 12.5f;
    [SerializeField, Min(1f)] private float effectFontSize = 9.5f;

    [Header("Pattern/Icon Layout")]
    [SerializeField] private Rect attackPatternRect = new Rect(0.07f, 0.035f, 0.405f, 0.255f);
    [SerializeField] private Rect typeIconRect = new Rect(0.115f, 0.052f, 0.35f, 0.235f);
    [SerializeField] private Color attackGridLineColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float attackGridLineThickness = 1f;
    [SerializeField, Min(0.1f)] private float patternIconInset = 1.5f;
    [SerializeField, Min(1)] private int maxStackedMoveIcons = 4;
    [SerializeField] private Vector2 moveIconStackOffset = new Vector2(0.12f, -0.05f);
    [SerializeField] private Vector2 moveIconNormalizedSize = new Vector2(0.78f, 0.92f);
    [SerializeField] private float moveIconBaseRotation = 0f;

    [Header("Generated Slots")]
    [SerializeField] private RectTransform artworkSlot;
    [FormerlySerializedAs("mainArtworkImage")]
    [SerializeField] private Image artworkImage;
    [FormerlySerializedAs("fullCardImage")]
    [SerializeField] private Image frameImage;
    [FormerlySerializedAs("arrowImage")]
    [SerializeField] private Image legacyArrowImage;
    [SerializeField] private RectTransform attackPatternRoot;
    [SerializeField] private RectTransform typeIconRoot;
    [SerializeField] private bool hideEmptyImages = true;

    private readonly List<Image> attackPatternCells = new();
    private readonly List<Image> attackGridLines = new();
    private readonly List<Image> typeIcons = new();
    private bool triedAutoAssignSprites;

    public void Bind(BaseCardSO card, Sprite fallbackFullCardSprite = null)
    {
        EnsureGeneratedTemplate();
        TryAutoAssignSprites();

        if (card == null)
        {
            Clear();
            SetImage(frameImage, fallbackFullCardSprite, false);
            return;
        }

        bool isSupport = IsSupport(card);
        SupportCardSO supportCard = card as SupportCardSO;
        bool showsTrapPattern = supportCard != null && HasTrapPattern(supportCard);
        Sprite frameSprite = GetFrameSprite(card);
        SetImage(frameImage, frameSprite != null ? frameSprite : fallbackFullCardSprite, false);
        SetArtwork(GetArtworkSprite(card));

        if (costText) costText.text = card.cost.ToString();
        if (nameText) nameText.text = ResolveCardTitle(card);
        if (effectText)
        {
            effectText.text = card.EffectText ?? string.Empty;
            SetNormalizedRect((RectTransform)effectText.transform, isSupport && !showsTrapPattern ? supportEffectTextRect : effectTextRect);
        }

        ClearCardSpecificVisuals();

        if (card is AttackCardSO || card.type == CardType.Attack)
            ShowAttackPattern(card as AttackCardSO);
        else if (showsTrapPattern)
            ShowTrapPattern(supportCard);
        else if (card is MoveCardSO || card.type == CardType.Move)
            ShowMoveIcon(card as MoveCardSO);
        else if (card is DrawCardSO || card.type == CardType.Draw)
            ShowDrawIcon(card as DrawCardSO);

        RefreshLayerOrder();
    }

    public void Clear()
    {
        EnsureGeneratedTemplate();

        if (costText) costText.text = string.Empty;
        if (nameText) nameText.text = string.Empty;
        if (effectText) effectText.text = string.Empty;

        SetImage(frameImage, null, false);
        SetArtwork(null);
        ClearCardSpecificVisuals();
    }

    public void OverrideTextFontSizes(float newCornerFontSize, float newEffectFontSize)
    {
        cornerFontSize = Mathf.Max(1f, newCornerFontSize);
        effectFontSize = Mathf.Max(1f, newEffectFontSize);

        EnsureGeneratedTemplate();
        ConfigureText(costText, cornerFontSize, TextAlignmentOptions.Center);
        ConfigureText(nameText, cornerFontSize, TextAlignmentOptions.MidlineLeft);
        ConfigureText(effectText, effectFontSize, TextAlignmentOptions.TopLeft);
    }

    public void CopyVisualSettingsFrom(CardTemplateView source)
    {
        if (!source || source == this)
            return;

        attackFrameSprite = source.attackFrameSprite;
        moveDrawFrameSprite = source.moveDrawFrameSprite;
        supportFrameSprite = source.supportFrameSprite;
        attackPatternIconSprite = source.attackPatternIconSprite;
        trapPatternIconSprite = source.trapPatternIconSprite;
        drawIconSprite = source.drawIconSprite;
        drawSecondaryIconSprite = source.drawSecondaryIconSprite;
        moveIconSprite = source.moveIconSprite;
        testArtworkSprite = source.testArtworkSprite;
        useCardMainArtwork = source.useCardMainArtwork;
        cardArtworkResourcesFolder = source.cardArtworkResourcesFolder;
        artworkRect = source.artworkRect;
        artworkFillScale = source.artworkFillScale;
        textColor = source.textColor;
        costTextRect = source.costTextRect;
        nameTextRect = source.nameTextRect;
        effectTextRect = source.effectTextRect;
        supportEffectTextRect = source.supportEffectTextRect;
        cornerFontSize = source.cornerFontSize;
        effectFontSize = source.effectFontSize;
        attackPatternRect = source.attackPatternRect;
        typeIconRect = source.typeIconRect;
        attackGridLineColor = source.attackGridLineColor;
        attackGridLineThickness = source.attackGridLineThickness;
        patternIconInset = source.patternIconInset;
        maxStackedMoveIcons = source.maxStackedMoveIcons;
        moveIconStackOffset = source.moveIconStackOffset;
        moveIconNormalizedSize = source.moveIconNormalizedSize;
        moveIconBaseRotation = source.moveIconBaseRotation;
        hideEmptyImages = source.hideEmptyImages;
        triedAutoAssignSprites = false;
    }

    private void EnsureGeneratedTemplate()
    {
        PrepareRootGraphic();

        if (frameImage && frameImage.transform == transform)
            frameImage = null;
        if (artworkImage && artworkImage.transform == transform)
            artworkImage = null;

        artworkSlot = EnsureRectTransform(artworkSlot, "CardArtworkSlot", transform);
        SetNormalizedRect(artworkSlot, artworkRect);

        artworkImage = EnsureImage(artworkImage, "CardArtwork", artworkSlot);
        SetArtworkRectDefaults((RectTransform)artworkImage.transform);

        frameImage = EnsureImage(frameImage, "CardFrame", transform);
        SetNormalizedRect((RectTransform)frameImage.transform, new Rect(0f, 0f, 1f, 1f));

        attackPatternRoot = EnsureRectTransform(attackPatternRoot, "AttackPattern", transform);
        SetNormalizedRect(attackPatternRoot, attackPatternRect);

        typeIconRoot = EnsureRectTransform(typeIconRoot, "TypeIcons", transform);
        SetNormalizedRect(typeIconRoot, typeIconRect);

        costText = EnsureText(costText, "CostText", transform);
        SetNormalizedRect((RectTransform)costText.transform, costTextRect);
        ConfigureText(costText, cornerFontSize, TextAlignmentOptions.Center);

        nameText = EnsureText(nameText, "NameText", transform);
        SetNormalizedRect((RectTransform)nameText.transform, nameTextRect);
        ConfigureText(nameText, cornerFontSize, TextAlignmentOptions.MidlineLeft);

        effectText = EnsureText(effectText, "EffectText", transform);
        SetNormalizedRect((RectTransform)effectText.transform, effectTextRect);
        ConfigureText(effectText, effectFontSize, TextAlignmentOptions.TopLeft);

        if (legacyArrowImage)
            legacyArrowImage.enabled = false;

        RefreshLayerOrder();
    }

    private void PrepareRootGraphic()
    {
        Image rootImage = GetComponent<Image>();
        if (!rootImage)
            rootImage = gameObject.AddComponent<Image>();

        rootImage.sprite = null;
        rootImage.color = new Color(1f, 1f, 1f, 0f);
        rootImage.preserveAspect = false;
    }

    private void RefreshLayerOrder()
    {
        if (artworkSlot) artworkSlot.SetAsFirstSibling();
        if (frameImage) frameImage.transform.SetAsLastSibling();
        if (attackPatternRoot) attackPatternRoot.SetAsLastSibling();
        if (typeIconRoot) typeIconRoot.SetAsLastSibling();
        if (costText) costText.transform.SetAsLastSibling();
        if (nameText) nameText.transform.SetAsLastSibling();
        if (effectText) effectText.transform.SetAsLastSibling();
    }

    private void TryAutoAssignSprites()
    {
        if (triedAutoAssignSprites)
            return;

        triedAutoAssignSprites = true;

        if (!attackFrameSprite) attackFrameSprite = LoadSprite("card3", "card3_0");
        if (!moveDrawFrameSprite) moveDrawFrameSprite = LoadSprite("card3", "card3_1");
        if (!supportFrameSprite) supportFrameSprite = LoadSprite("card3", "card3_2");
        if (!attackPatternIconSprite) attackPatternIconSprite = LoadSprite("icon", "icon_0");
        if (!trapPatternIconSprite) trapPatternIconSprite = attackPatternIconSprite ? attackPatternIconSprite : LoadSprite("icon", "icon_0");
        if (!drawIconSprite) drawIconSprite = LoadSprite("icon", "icon_2");
        if (!moveIconSprite) moveIconSprite = LoadSprite("icon", "icon_3");
        if (!drawSecondaryIconSprite) drawSecondaryIconSprite = LoadSprite("icon", "icon_10");
        if (!testArtworkSprite) testArtworkSprite = LoadSprite("test_image", "test_image");
    }

    private Sprite GetFrameSprite(BaseCardSO card)
    {
        if (card == null)
            return null;

        if (card is AttackCardSO || card.type == CardType.Attack)
            return attackFrameSprite;

        if (card is MoveCardSO || card is DrawCardSO || card.type == CardType.Move || card.type == CardType.Draw)
            return moveDrawFrameSprite;

        if (card is SupportCardSO || card.type == CardType.Support)
            return supportFrameSprite;

        return moveDrawFrameSprite;
    }

    private Sprite GetArtworkSprite(BaseCardSO card)
    {
        if (card != null && useCardMainArtwork)
        {
            if (card.mainArtwork)
                return card.mainArtwork;

            Sprite resourcesArtwork = LoadCardArtwork(card.id);
            if (resourcesArtwork)
                return resourcesArtwork;
        }

        return testArtworkSprite != null ? testArtworkSprite : card != null ? card.mainArtwork : null;
    }

    private Sprite LoadCardArtwork(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(cardArtworkResourcesFolder))
            return null;

        string folder = cardArtworkResourcesFolder.Trim().Trim('/');
        return Resources.Load<Sprite>($"{folder}/{cardId}");
    }

    private static string ResolveCardTitle(BaseCardSO card)
    {
        if (card == null)
            return string.Empty;

        string displayName = string.IsNullOrWhiteSpace(card.displayName) ? string.Empty : card.displayName.Trim();
        if (!IsTechnicalCardName(displayName, card.id))
            return displayName;

        string emotionTitle = ExtractBracketTitle(card.display);
        if (!string.IsNullOrWhiteSpace(emotionTitle))
            return emotionTitle;

        string firstDisplayLine = ExtractFirstDisplayLine(card.display);
        if (!string.IsNullOrWhiteSpace(firstDisplayLine))
            return firstDisplayLine;

        return !string.IsNullOrWhiteSpace(displayName) ? displayName : card.id;
    }

    private static bool IsTechnicalCardName(string displayName, string id)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return true;

        if (!string.IsNullOrWhiteSpace(id) && displayName == id)
            return true;

        return LooksLikeNumberedId(displayName, "AttackCard")
            || LooksLikeNumberedId(displayName, "MoveCard")
            || LooksLikeNumberedId(displayName, "DrawCard")
            || LooksLikeNumberedId(displayName, "SupportCard")
            || LooksLikeNumberedId(displayName, "Card");
    }

    private static bool LooksLikeNumberedId(string value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(prefix))
            return false;

        if (value.Length == prefix.Length)
            return true;

        for (int i = prefix.Length; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
                return false;
        }

        return true;
    }

    private static string ExtractBracketTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        int start = value.IndexOf('[');
        int end = start >= 0 ? value.IndexOf(']', start + 1) : -1;
        if (start < 0 || end <= start + 1)
            return string.Empty;

        return value.Substring(start + 1, end - start - 1).Trim();
    }

    private static string ExtractFirstDisplayLine(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string firstLine = value.Split('\n')[0].Trim();
        return firstLine.Trim('[', ']', ' ', '\r', '\t');
    }

    private static bool IsSupport(BaseCardSO card)
    {
        return card is SupportCardSO || (card != null && card.type == CardType.Support);
    }

    private void SetArtwork(Sprite sprite)
    {
        SetImage(artworkImage, sprite, true);

        if (!artworkImage)
            return;

        RectTransform rt = (RectTransform)artworkImage.transform;
        SetArtworkRectDefaults(rt);
        rt.localScale = Vector3.one * Mathf.Max(0.1f, artworkFillScale);

        AspectRatioFitter fitter = artworkImage.GetComponent<AspectRatioFitter>();
        if (!fitter)
            fitter = artworkImage.gameObject.AddComponent<AspectRatioFitter>();

        fitter.enabled = sprite != null;
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite ? Mathf.Max(0.01f, sprite.rect.width / sprite.rect.height) : 1f;
    }

    private static void SetArtworkRectDefaults(RectTransform rt)
    {
        if (!rt)
            return;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private void ClearCardSpecificVisuals()
    {
        if (attackPatternRoot) attackPatternRoot.gameObject.SetActive(false);
        if (typeIconRoot) typeIconRoot.gameObject.SetActive(false);

        for (int i = 0; i < attackPatternCells.Count; i++)
            if (attackPatternCells[i]) attackPatternCells[i].gameObject.SetActive(false);

        for (int i = 0; i < attackGridLines.Count; i++)
            if (attackGridLines[i]) attackGridLines[i].gameObject.SetActive(false);

        for (int i = 0; i < typeIcons.Count; i++)
            if (typeIcons[i]) typeIcons[i].gameObject.SetActive(false);
    }

    private void ShowAttackPattern(AttackCardSO attackCard)
    {
        if (!attackPatternRoot)
            return;

        attackPatternRoot.gameObject.SetActive(true);
        SetNormalizedRect(attackPatternRoot, attackPatternRect);
        EnsureAttackGridLines();
        EnsureAttackPatternCells();

        for (int i = 0; i < attackGridLines.Count; i++)
            if (attackGridLines[i]) attackGridLines[i].gameObject.SetActive(true);

        bool[] mask = BuildAttackMask(attackCard);
        for (int i = 0; i < 16; i++)
        {
            Image cell = attackPatternCells[i];
            if (!cell)
                continue;

            cell.sprite = attackPatternIconSprite;
            cell.color = Color.white;
            cell.preserveAspect = true;
            cell.raycastTarget = false;
            cell.gameObject.SetActive(mask[i] && attackPatternIconSprite != null);
        }
    }

    private void ShowTrapPattern(SupportCardSO supportCard)
    {
        if (!attackPatternRoot)
            return;

        attackPatternRoot.gameObject.SetActive(true);
        SetNormalizedRect(attackPatternRoot, attackPatternRect);
        EnsureAttackGridLines();
        EnsureAttackPatternCells();

        for (int i = 0; i < attackGridLines.Count; i++)
            if (attackGridLines[i]) attackGridLines[i].gameObject.SetActive(true);

        Sprite iconSprite = trapPatternIconSprite ? trapPatternIconSprite : attackPatternIconSprite;
        bool[] mask = BuildTrapMask(supportCard);
        for (int i = 0; i < 16; i++)
        {
            Image cell = attackPatternCells[i];
            if (!cell)
                continue;

            cell.sprite = iconSprite;
            cell.color = Color.white;
            cell.preserveAspect = true;
            cell.raycastTarget = false;
            cell.transform.localRotation = Quaternion.identity;
            cell.gameObject.SetActive(mask[i] && iconSprite != null);
        }
    }

    private void ShowMoveIcon(MoveCardSO moveCard)
    {
        if (!typeIconRoot)
            return;

        typeIconRoot.gameObject.SetActive(true);
        SetNormalizedRect(typeIconRoot, typeIconRect);

        int amount = Mathf.Clamp(moveCard != null ? moveCard.amount : 1, 1, Mathf.Max(1, maxStackedMoveIcons));
        for (int i = 0; i < amount; i++)
        {
            Image icon = GetTypeIcon(i);
            if (!icon)
                continue;

            Rect iconRect = BuildStackedIconRect(i, amount);
            SetNormalizedRect((RectTransform)icon.transform, iconRect);
            icon.sprite = moveIconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.transform.localRotation = Quaternion.Euler(0f, 0f, moveIconBaseRotation + GetMoveRotation(moveCard));
            icon.gameObject.SetActive(moveIconSprite != null);
        }
    }

    private void ShowDrawIcon(DrawCardSO drawCard)
    {
        if (!typeIconRoot)
            return;

        typeIconRoot.gameObject.SetActive(true);
        SetNormalizedRect(typeIconRoot, typeIconRect);

        Sprite iconSprite = drawCard != null && drawCard.drawMode == DrawMode.AntiDraw
            ? drawSecondaryIconSprite
            : drawIconSprite;

        Image icon = GetTypeIcon(0);
        if (icon)
        {
            SetNormalizedRect((RectTransform)icon.transform, new Rect(0.08f, 0.05f, 0.84f, 0.9f));
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.transform.localRotation = Quaternion.identity;
            icon.gameObject.SetActive(iconSprite != null);
        }
    }

    private Rect BuildStackedIconRect(int index, int count)
    {
        Vector2 size = moveIconNormalizedSize;
        float middleIndex = (Mathf.Max(1, count) - 1) * 0.5f;
        Vector2 center = new Vector2(0.5f, 0.5f) + moveIconStackOffset * (index - middleIndex);
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    private static float GetMoveRotation(MoveCardSO moveCard)
    {
        if (!moveCard)
            return 0f;

        switch (moveCard.where)
        {
            case Dir4.Right:
                return -90f;
            case Dir4.Down:
                return 180f;
            case Dir4.Left:
                return 90f;
            default:
                return 0f;
        }
    }

    private bool[] BuildAttackMask(AttackCardSO attackCard)
    {
        bool[] mask = new bool[16];
        if (!attackCard)
            return mask;

        MergeMask(mask, attackCard.hitMask);

        if (attackCard.waves != null)
        {
            for (int i = 0; i < attackCard.waves.Length; i++)
            {
                if (attackCard.waves[i] != null)
                    MergeMask(mask, attackCard.waves[i].hitMask);
            }
        }

        return mask;
    }

    private static bool HasTrapPattern(SupportCardSO supportCard)
    {
        if (!supportCard || supportCard.effects == null)
            return false;

        for (int i = 0; i < supportCard.effects.Count; i++)
        {
            SupportEffect effect = supportCard.effects[i];
            if (effect == null || effect.category != SupportEffectCategory.Trap || effect.trapPlacements == null)
                continue;

            for (int j = 0; j < effect.trapPlacements.Count; j++)
            {
                SupportTrapPlacement placement = effect.trapPlacements[j];
                if (placement != null && placement.gridMask != null && !placement.gridMask.IsEmpty())
                    return true;
            }
        }

        return false;
    }

    private static bool[] BuildTrapMask(SupportCardSO supportCard)
    {
        bool[] mask = new bool[16];
        if (!supportCard || supportCard.effects == null)
            return mask;

        for (int i = 0; i < supportCard.effects.Count; i++)
        {
            SupportEffect effect = supportCard.effects[i];
            if (effect == null || effect.category != SupportEffectCategory.Trap || effect.trapPlacements == null)
                continue;

            for (int j = 0; j < effect.trapPlacements.Count; j++)
            {
                SupportTrapPlacement placement = effect.trapPlacements[j];
                MergeTrapMask(mask, placement != null ? placement.gridMask : null);
            }
        }

        return mask;
    }

    private static void MergeTrapMask(bool[] target, SupportGridMask source)
    {
        if (target == null || target.Length < 16 || source == null || source.IsEmpty())
            return;

        for (int row = 1; row <= 4; row++)
        {
            for (int col = 1; col <= 4; col++)
            {
                Vector2Int rc = new Vector2Int(row, col);
                int index = SupportGridMask.ToIndex(rc);
                if (index >= 0 && source.Contains(rc))
                    target[index] = true;
            }
        }
    }

    private static void MergeMask(bool[] target, AttackGridMask source)
    {
        if (target == null || target.Length < 16 || source == null || source.IsEmpty())
            return;

        bool[] sourceMask = new bool[16];
        source.CopyTo(sourceMask);
        for (int i = 0; i < 16; i++)
            target[i] |= sourceMask[i];
    }

    private void EnsureAttackPatternCells()
    {
        while (attackPatternCells.Count < 16)
            attackPatternCells.Add(CreateGeneratedImage($"PatternCell_{attackPatternCells.Count:00}", attackPatternRoot));

        for (int i = 0; i < 16; i++)
        {
            Image cell = attackPatternCells[i];
            if (!cell)
                continue;

            int col = i % 4;
            int row = i / 4;
            RectTransform rt = (RectTransform)cell.transform;
            rt.anchorMin = new Vector2(col / 4f, 1f - (row + 1) / 4f);
            rt.anchorMax = new Vector2((col + 1) / 4f, 1f - row / 4f);
            rt.offsetMin = Vector2.one * patternIconInset;
            rt.offsetMax = -Vector2.one * patternIconInset;
            rt.pivot = new Vector2(0.5f, 0.5f);
            cell.transform.SetAsLastSibling();
        }
    }

    private void EnsureAttackGridLines()
    {
        while (attackGridLines.Count < 10)
            attackGridLines.Add(CreateGeneratedImage($"PatternGridLine_{attackGridLines.Count:00}", attackPatternRoot));

        for (int i = 0; i < 5; i++)
        {
            Image vertical = attackGridLines[i];
            ConfigureLine(vertical, new Vector2(i / 4f, 0f), new Vector2(i / 4f, 1f), new Vector2(attackGridLineThickness, 0f));

            Image horizontal = attackGridLines[i + 5];
            ConfigureLine(horizontal, new Vector2(0f, i / 4f), new Vector2(1f, i / 4f), new Vector2(0f, attackGridLineThickness));
        }
    }

    private void ConfigureLine(Image image, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        if (!image)
            return;

        RectTransform rt = (RectTransform)image.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        rt.pivot = new Vector2(0.5f, 0.5f);
        image.sprite = null;
        image.color = attackGridLineColor;
        image.raycastTarget = false;
    }

    private Image GetTypeIcon(int index)
    {
        while (typeIcons.Count <= index)
            typeIcons.Add(CreateGeneratedImage($"TypeIcon_{typeIcons.Count:00}", typeIconRoot));

        Image icon = typeIcons[index];
        if (icon)
            icon.transform.SetAsLastSibling();
        return icon;
    }

    private Image EnsureImage(Image image, string name, Transform parent)
    {
        if (image && image.transform != transform)
            return image;

        Transform existing = parent.Find(name);
        if (existing && existing.TryGetComponent(out Image existingImage))
            return existingImage;

        return CreateGeneratedImage(name, parent);
    }

    private Image CreateGeneratedImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    private RectTransform EnsureRectTransform(RectTransform rectTransform, string name, Transform parent)
    {
        if (rectTransform)
            return rectTransform;

        Transform existing = parent.Find(name);
        if (existing && existing is RectTransform existingRect)
            return existingRect;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private TMP_Text EnsureText(TMP_Text text, string name, Transform parent)
    {
        if (text)
            return text;

        Transform existing = parent.Find(name);
        if (existing && existing.TryGetComponent(out TMP_Text existingText))
            return existingText;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        return tmp;
    }

    private void ConfigureText(TMP_Text text, float fontSize, TextAlignmentOptions alignment)
    {
        if (!text)
            return;

        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = Mathf.Max(1f, fontSize * 0.55f);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.color = textColor;
        text.margin = Vector4.zero;
    }

    private static void SetNormalizedRect(RectTransform rt, Rect normalizedRect)
    {
        if (!rt)
            return;

        rt.anchorMin = new Vector2(normalizedRect.xMin, normalizedRect.yMin);
        rt.anchorMax = new Vector2(normalizedRect.xMax, normalizedRect.yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private void SetImage(Image image, Sprite sprite, bool preserveAspect)
    {
        if (!image)
            return;

        image.sprite = sprite;
        image.enabled = !hideEmptyImages || sprite != null;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        if (sprite)
            image.color = Color.white;
    }

    private static Sprite LoadSprite(string assetName, string spriteName)
    {
        Sprite[] resourceSprites = Resources.LoadAll<Sprite>($"my_asset/CardTemplate/{assetName}");
        for (int i = 0; i < resourceSprites.Length; i++)
        {
            if (resourceSprites[i] && resourceSprites[i].name == spriteName)
                return resourceSprites[i];
        }

        Sprite resourceSprite = Resources.Load<Sprite>($"my_asset/CardTemplate/{spriteName}");
        if (resourceSprite)
            return resourceSprite;

        resourceSprites = Resources.LoadAll<Sprite>($"my_asset/{assetName}");
        for (int i = 0; i < resourceSprites.Length; i++)
        {
            if (resourceSprites[i] && resourceSprites[i].name == spriteName)
                return resourceSprites[i];
        }

        resourceSprite = Resources.Load<Sprite>($"my_asset/{spriteName}");
        if (resourceSprite)
            return resourceSprite;

#if UNITY_EDITOR
        string path = $"Assets/my_asset/{assetName}.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }
}
