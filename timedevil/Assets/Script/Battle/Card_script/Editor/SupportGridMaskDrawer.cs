using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SupportGridMask))]
public class SupportGridMaskDrawer : PropertyDrawer
{
    private const int Size = 4;
    private const int CellCount = Size * Size;
    private const float CellSize = 22f;
    private const float Gap = 3f;
    private const float ButtonWidth = 54f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty cells = property.FindPropertyRelative("cells");
        if (cells != null && cells.arraySize != CellCount)
            cells.arraySize = CellCount;

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        if (!property.isExpanded || cells == null)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        float indent = EditorGUI.indentLevel * 15f;
        float startX = position.x + indent;
        float startY = position.y + EditorGUIUtility.singleLineHeight + Gap;

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                int index = row * Size + col;
                Rect cellRect = new Rect(
                    startX + col * (CellSize + Gap),
                    startY + row * (CellSize + Gap),
                    CellSize,
                    CellSize);

                SerializedProperty cell = cells.GetArrayElementAtIndex(index);
                cell.boolValue = GUI.Toggle(cellRect, cell.boolValue, GUIContent.none, EditorStyles.miniButton);
            }
        }

        float buttonY = startY + Size * (CellSize + Gap) + Gap;
        Rect clearRect = new Rect(startX, buttonY, ButtonWidth, EditorGUIUtility.singleLineHeight);
        Rect fillRect = new Rect(startX + ButtonWidth + Gap, buttonY, ButtonWidth, EditorGUIUtility.singleLineHeight);

        if (GUI.Button(clearRect, "Clear"))
            SetAll(cells, false);
        if (GUI.Button(fillRect, "All"))
            SetAll(cells, true);

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        return EditorGUIUtility.singleLineHeight
            + Gap
            + Size * (CellSize + Gap)
            + EditorGUIUtility.singleLineHeight
            + Gap;
    }

    private static void SetAll(SerializedProperty cells, bool value)
    {
        for (int i = 0; i < cells.arraySize; i++)
            cells.GetArrayElementAtIndex(i).boolValue = value;
    }
}
