using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SupportEffect))]
public class SupportEffectDrawer : PropertyDrawer
{
    private const float Gap = 3f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y = position.y + EditorGUIUtility.singleLineHeight + Gap;
        SerializedProperty category = property.FindPropertyRelative("category");
        DrawProperty(position, ref y, category);

        switch ((SupportEffectCategory)category.enumValueIndex)
        {
            case SupportEffectCategory.Cost:
                DrawCostFields(position, ref y, property);
                break;
            case SupportEffectCategory.HP:
                DrawHpFields(position, ref y, property);
                break;
            case SupportEffectCategory.Stat:
                DrawStatFields(position, ref y, property);
                break;
            case SupportEffectCategory.Trap:
                DrawTrapFields(position, ref y, property);
                break;
            case SupportEffectCategory.Guard:
                DrawGuardFields(position, ref y, property);
                break;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
            return height;

        height += Gap;

        SerializedProperty category = property.FindPropertyRelative("category");
        AddPropertyHeight(ref height, category);

        switch ((SupportEffectCategory)category.enumValueIndex)
        {
            case SupportEffectCategory.Cost:
                AddCostHeight(ref height, property);
                break;
            case SupportEffectCategory.HP:
                AddHpHeight(ref height, property);
                break;
            case SupportEffectCategory.Stat:
                AddStatHeight(ref height, property);
                break;
            case SupportEffectCategory.Trap:
                AddTrapHeight(ref height, property);
                break;
            case SupportEffectCategory.Guard:
                AddGuardHeight(ref height, property);
                break;
        }

        return height;
    }

    public override bool CanCacheInspectorGUI(SerializedProperty property)
    {
        return false;
    }

    private static void DrawCostFields(Rect position, ref float y, SerializedProperty property)
    {
        SerializedProperty costType = property.FindPropertyRelative("costType");
        DrawProperty(position, ref y, costType);

        switch ((SupportCostEffectType)costType.enumValueIndex)
        {
            case SupportCostEffectType.NextCardFree:
                DrawProperty(position, ref y, property.FindPropertyRelative("freeCardCount"));
                break;
            case SupportCostEffectType.GainCostByPayingHpPercent:
                DrawProperty(position, ref y, property.FindPropertyRelative("hpCostPercent"));
                DrawProperty(position, ref y, property.FindPropertyRelative("costGainAmount"));
                DrawProperty(position, ref y, property.FindPropertyRelative("allowCostOverMax"));
                DrawProperty(position, ref y, property.FindPropertyRelative("hpPaymentCanDefeat"));
                break;
            case SupportCostEffectType.GainCostByDiscardHand:
                DrawProperty(position, ref y, property.FindPropertyRelative("discardHandCount"));
                DrawProperty(position, ref y, property.FindPropertyRelative("costGainAmount"));
                DrawProperty(position, ref y, property.FindPropertyRelative("allowCostOverMax"));
                break;
        }
    }

    private static void DrawHpFields(Rect position, ref float y, SerializedProperty property)
    {
        DrawProperty(position, ref y, property.FindPropertyRelative("target"));

        SerializedProperty hpType = property.FindPropertyRelative("hpType");
        DrawProperty(position, ref y, hpType);
        DrawProperty(position, ref y, property.FindPropertyRelative("hpAmount"));

        if ((SupportHpEffectType)hpType.enumValueIndex == SupportHpEffectType.TurnStartChange)
            DrawProperty(position, ref y, property.FindPropertyRelative("hpTurns"));
    }

    private static void DrawStatFields(Rect position, ref float y, SerializedProperty property)
    {
        DrawProperty(position, ref y, property.FindPropertyRelative("target"));
        DrawProperty(position, ref y, property.FindPropertyRelative("statType"));
        DrawProperty(position, ref y, property.FindPropertyRelative("statAmount"));
        DrawProperty(position, ref y, property.FindPropertyRelative("statTurns"));
    }

    private static void DrawTrapFields(Rect position, ref float y, SerializedProperty property)
    {
        DrawProperty(position, ref y, property.FindPropertyRelative("trapPlacements"), true);
        DrawProperty(position, ref y, property.FindPropertyRelative("trapDamage"));
        DrawProperty(position, ref y, property.FindPropertyRelative("trapDurationTurns"));
        DrawProperty(position, ref y, property.FindPropertyRelative("triggerImmediatelyIfOccupied"));
        DrawProperty(position, ref y, property.FindPropertyRelative("removeAfterTrigger"));
    }

    private static void DrawGuardFields(Rect position, ref float y, SerializedProperty property)
    {
        DrawProperty(position, ref y, property.FindPropertyRelative("target"));
        DrawProperty(position, ref y, property.FindPropertyRelative("guardTurns"));
    }

    private static void AddCostHeight(ref float height, SerializedProperty property)
    {
        SerializedProperty costType = property.FindPropertyRelative("costType");
        AddPropertyHeight(ref height, costType);

        switch ((SupportCostEffectType)costType.enumValueIndex)
        {
            case SupportCostEffectType.NextCardFree:
                AddPropertyHeight(ref height, property.FindPropertyRelative("freeCardCount"));
                break;
            case SupportCostEffectType.GainCostByPayingHpPercent:
                AddPropertyHeight(ref height, property.FindPropertyRelative("hpCostPercent"));
                AddPropertyHeight(ref height, property.FindPropertyRelative("costGainAmount"));
                AddPropertyHeight(ref height, property.FindPropertyRelative("allowCostOverMax"));
                AddPropertyHeight(ref height, property.FindPropertyRelative("hpPaymentCanDefeat"));
                break;
            case SupportCostEffectType.GainCostByDiscardHand:
                AddPropertyHeight(ref height, property.FindPropertyRelative("discardHandCount"));
                AddPropertyHeight(ref height, property.FindPropertyRelative("costGainAmount"));
                AddPropertyHeight(ref height, property.FindPropertyRelative("allowCostOverMax"));
                break;
        }
    }

    private static void AddHpHeight(ref float height, SerializedProperty property)
    {
        AddPropertyHeight(ref height, property.FindPropertyRelative("target"));

        SerializedProperty hpType = property.FindPropertyRelative("hpType");
        AddPropertyHeight(ref height, hpType);
        AddPropertyHeight(ref height, property.FindPropertyRelative("hpAmount"));

        if ((SupportHpEffectType)hpType.enumValueIndex == SupportHpEffectType.TurnStartChange)
            AddPropertyHeight(ref height, property.FindPropertyRelative("hpTurns"));
    }

    private static void AddStatHeight(ref float height, SerializedProperty property)
    {
        AddPropertyHeight(ref height, property.FindPropertyRelative("target"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("statType"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("statAmount"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("statTurns"));
    }

    private static void AddTrapHeight(ref float height, SerializedProperty property)
    {
        AddPropertyHeight(ref height, property.FindPropertyRelative("trapPlacements"), true);
        AddPropertyHeight(ref height, property.FindPropertyRelative("trapDamage"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("trapDurationTurns"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("triggerImmediatelyIfOccupied"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("removeAfterTrigger"));
    }

    private static void AddGuardHeight(ref float height, SerializedProperty property)
    {
        AddPropertyHeight(ref height, property.FindPropertyRelative("target"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("guardTurns"));
    }

    private static void DrawProperty(Rect position, ref float y, SerializedProperty property, bool includeChildren = false)
    {
        if (property == null)
            return;

        float height = EditorGUI.GetPropertyHeight(property, includeChildren);
        Rect rect = new Rect(position.x, y, position.width, height);
        EditorGUI.PropertyField(rect, property, includeChildren);
        y += height + Gap;
    }

    private static void AddPropertyHeight(ref float height, SerializedProperty property, bool includeChildren = false)
    {
        if (property == null)
            return;

        height += EditorGUI.GetPropertyHeight(property, includeChildren) + Gap;
    }
}
