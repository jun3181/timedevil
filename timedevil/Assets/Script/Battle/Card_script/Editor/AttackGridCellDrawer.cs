using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackGridCell))]
public class AttackGridCellDrawer : PropertyDrawer
{
    private const int Size = 4;
    private const float CellSize = 22f;
    private const float Gap = 3f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty index = property.FindPropertyRelative("index");
        if (index == null)
        {
            EditorGUI.EndProperty();
            return;
        }

        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        int selected = Mathf.Clamp(index.intValue, 0, 15);
        float startX = position.x + EditorGUIUtility.labelWidth;
        float startY = position.y;

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                int cellIndex = row * Size + col;
                Rect cellRect = new Rect(
                    startX + col * (CellSize + Gap),
                    startY + row * (CellSize + Gap),
                    CellSize,
                    CellSize);

                bool isSelected = selected == cellIndex;
                if (GUI.Toggle(cellRect, isSelected, isSelected ? "X" : string.Empty, EditorStyles.miniButton))
                    index.intValue = cellIndex;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return Size * (CellSize + Gap) - Gap;
    }
}
