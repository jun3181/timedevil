using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackCardSO.Wave))]
public class AttackWaveDrawer : PropertyDrawer
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
        SerializedProperty castType = property.FindPropertyRelative("castType");
        DrawProperty(position, ref y, castType);

        DrawProperty(position, ref y, property.FindPropertyRelative("delayBefore"));
        DrawProperty(position, ref y, property.FindPropertyRelative("delayAfter"));

        if ((AttackCastType)castType.enumValueIndex == AttackCastType.Projectile)
            DrawProjectileFields(position, ref y, property);
        else
            DrawInstantFields(position, ref y, property);

        DrawCommonFxFields(position, ref y, property);

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
            return height;

        height += Gap;

        SerializedProperty castType = property.FindPropertyRelative("castType");
        AddPropertyHeight(ref height, castType);
        AddPropertyHeight(ref height, property.FindPropertyRelative("delayBefore"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("delayAfter"));

        if ((AttackCastType)castType.enumValueIndex == AttackCastType.Projectile)
            AddProjectileHeight(ref height, property);
        else
            AddInstantHeight(ref height, property);

        AddCommonFxHeight(ref height, property);

        return height;
    }

    public override bool CanCacheInspectorGUI(SerializedProperty property)
    {
        return false;
    }

    private static void DrawInstantFields(Rect position, ref float y, SerializedProperty property)
    {
        DrawProperty(position, ref y, property.FindPropertyRelative("hitMask"));
        DrawProperty(position, ref y, property.FindPropertyRelative("explosionPrefab"));
        DrawProperty(position, ref y, property.FindPropertyRelative("explosionLifetime"));
        DrawProperty(position, ref y, property.FindPropertyRelative("explosionScale"));
    }

    private static void DrawProjectileFields(Rect position, ref float y, SerializedProperty property)
    {
        DrawProjectileRoutes(position, ref y, property.FindPropertyRelative("projectileRoutes"));
        DrawProperty(position, ref y, property.FindPropertyRelative("projectilePrefab"));
        DrawProperty(position, ref y, property.FindPropertyRelative("projectileSpeed"));
        DrawProperty(position, ref y, property.FindPropertyRelative("projectileHitWidth"));
        DrawProperty(position, ref y, property.FindPropertyRelative("projectileHitHeight"));
        DrawProperty(position, ref y, property.FindPropertyRelative("destroyOnImpact"));
        DrawProperty(position, ref y, property.FindPropertyRelative("projectileScale"));
    }

    private static void DrawCommonFxFields(Rect position, ref float y, SerializedProperty property)
    {
        DrawProperty(position, ref y, property.FindPropertyRelative("sfx"));
        DrawProperty(position, ref y, property.FindPropertyRelative("sfxEveryHit"));
        DrawProperty(position, ref y, property.FindPropertyRelative("vfxPrefab"));
        DrawProperty(position, ref y, property.FindPropertyRelative("vfxEveryHit"));
        DrawProperty(position, ref y, property.FindPropertyRelative("vfxLifetime"));
        DrawProperty(position, ref y, property.FindPropertyRelative("clipKey"));
    }

    private static void AddInstantHeight(ref float height, SerializedProperty property)
    {
        AddPropertyHeight(ref height, property.FindPropertyRelative("hitMask"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("explosionPrefab"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("explosionLifetime"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("explosionScale"));
    }

    private static void AddProjectileHeight(ref float height, SerializedProperty property)
    {
        AddProjectileRoutesHeight(ref height, property.FindPropertyRelative("projectileRoutes"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("projectilePrefab"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("projectileSpeed"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("projectileHitWidth"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("projectileHitHeight"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("destroyOnImpact"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("projectileScale"));
    }

    private static void AddCommonFxHeight(ref float height, SerializedProperty property)
    {
        AddPropertyHeight(ref height, property.FindPropertyRelative("sfx"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("sfxEveryHit"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("vfxPrefab"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("vfxEveryHit"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("vfxLifetime"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("clipKey"));
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

    private static void DrawProjectileRoutes(Rect position, ref float y, SerializedProperty routes)
    {
        if (routes == null)
            return;

        Rect headerRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(headerRect, "Projectile Routes");
        Rect addRect = new Rect(position.xMax - 70f, y, 70f, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(addRect, "Add"))
        {
            int newIndex = routes.arraySize;
            routes.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newRoute = routes.GetArrayElementAtIndex(newIndex);
            SetNestedInt(newRoute, "from", "index", 0);
            SetNestedInt(newRoute, "to", "index", 0);
            SerializedProperty launchDelay = newRoute.FindPropertyRelative("launchDelay");
            if (launchDelay != null) launchDelay.floatValue = 0f;
        }
        y += EditorGUIUtility.singleLineHeight + Gap;

        EditorGUI.indentLevel++;
        for (int i = 0; i < routes.arraySize; i++)
        {
            SerializedProperty route = routes.GetArrayElementAtIndex(i);
            if (route == null) continue;

            Rect routeHeader = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(routeHeader, $"Route {i + 1}");

            Rect removeRect = new Rect(position.xMax - 70f, y, 70f, EditorGUIUtility.singleLineHeight);
            using (new EditorGUI.DisabledScope(routes.arraySize <= 1))
            {
                if (GUI.Button(removeRect, "Remove"))
                {
                    routes.DeleteArrayElementAtIndex(i);
                    y += EditorGUIUtility.singleLineHeight + Gap;
                    break;
                }
            }
            y += EditorGUIUtility.singleLineHeight + Gap;

            EditorGUI.indentLevel++;
            DrawProperty(position, ref y, route.FindPropertyRelative("from"), new GUIContent("From (Self Panel)"));
            DrawProperty(position, ref y, route.FindPropertyRelative("to"), new GUIContent("To (Opponent Panel)"));
            DrawProperty(position, ref y, route.FindPropertyRelative("launchDelay"));
            EditorGUI.indentLevel--;
        }
        EditorGUI.indentLevel--;
    }

    private static void AddProjectileRoutesHeight(ref float height, SerializedProperty routes)
    {
        if (routes == null)
            return;

        height += EditorGUIUtility.singleLineHeight + Gap;

        for (int i = 0; i < routes.arraySize; i++)
        {
            SerializedProperty route = routes.GetArrayElementAtIndex(i);
            if (route == null) continue;

            height += EditorGUIUtility.singleLineHeight + Gap;
            AddPropertyHeight(ref height, route.FindPropertyRelative("from"));
            AddPropertyHeight(ref height, route.FindPropertyRelative("to"));
            AddPropertyHeight(ref height, route.FindPropertyRelative("launchDelay"));
        }
    }

    private static void DrawProperty(Rect position, ref float y, SerializedProperty property, GUIContent label)
    {
        if (property == null)
            return;

        float height = EditorGUI.GetPropertyHeight(property, label, true);
        Rect rect = new Rect(position.x, y, position.width, height);
        EditorGUI.PropertyField(rect, property, label, true);
        y += height + Gap;
    }

    private static void SetNestedInt(SerializedProperty property, string childName, string nestedName, int value)
    {
        SerializedProperty child = property?.FindPropertyRelative(childName);
        SerializedProperty nested = child?.FindPropertyRelative(nestedName);
        if (nested != null) nested.intValue = value;
    }

    private static void AddPropertyHeight(ref float height, SerializedProperty property, bool includeChildren = false)
    {
        if (property == null)
            return;

        height += EditorGUI.GetPropertyHeight(property, includeChildren) + Gap;
    }
}
