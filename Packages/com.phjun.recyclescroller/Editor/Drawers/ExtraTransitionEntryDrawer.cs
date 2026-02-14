using RecycleScroll;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomPropertyDrawer(typeof(ExtraTransitionEntry))]
public class ExtraTransitionEntryDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Foldout + Transition (always visible)
        if (!property.isExpanded)
            return lineH;

        var transitionProp = property.FindPropertyRelative("m_transition");
        var transitionType = (eExtraTransition)transitionProp.enumValueIndex;

        // Transition + Target Graphic + Button + transition-specific fields
        int fieldCount = 3; // Transition + Target Graphic + Button
        switch (transitionType)
        {
            case eExtraTransition.ColorTint:
                fieldCount += 7; // 5 colors + multiplier + fade
                break;
            case eExtraTransition.SpriteSwap:
                fieldCount += 4; // 4 sprites
                break;
        }

        return fieldCount * lineH + (fieldCount - 1) * spacing + spacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var transitionProp = property.FindPropertyRelative("m_transition");
        var targetGraphicProp = property.FindPropertyRelative("m_targetGraphic");
        var colorsProp = property.FindPropertyRelative("m_colors");
        var spriteStateProp = property.FindPropertyRelative("m_spriteState");

        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float y = position.y;

        // Foldout + Transition enum on the same line
        var foldoutRect = new Rect(position.x, y, EditorGUIUtility.labelWidth, lineH);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, "Transition", true);

        var enumRect = new Rect(position.x + EditorGUIUtility.labelWidth, y,
            position.width - EditorGUIUtility.labelWidth, lineH);
        EditorGUI.PropertyField(enumRect, transitionProp, GUIContent.none);
        y += lineH + spacing;

        if (property.isExpanded)
        {
            var transitionType = (eExtraTransition)transitionProp.enumValueIndex;

            EditorGUI.indentLevel++;

            // Target Graphic (always visible when expanded)
            DrawField(position.x, ref y, position.width, lineH, spacing, targetGraphicProp, "Target Graphic");

            switch (transitionType)
            {
                case eExtraTransition.ColorTint:
                    DrawColorTintFields(position.x, ref y, position.width, lineH, spacing, colorsProp);
                    break;
                case eExtraTransition.SpriteSwap:
                    DrawSpriteSwapFields(position.x, ref y, position.width, lineH, spacing, spriteStateProp);
                    break;
            }

            // Copy Base Transition button
            var buttonRect = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, lineH));
            if (GUI.Button(buttonRect, "Copy Base Transition"))
            {
                CopyBaseTransitionToEntry(property);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    #region Copy Helpers

    internal static void CopyBaseTransitionToEntry(SerializedProperty entryProp)
    {
        var so = entryProp.serializedObject;
        var baseColors = so.FindProperty("m_Colors");
        var baseSpriteState = so.FindProperty("m_SpriteState");

        // ColorBlock
        CopyColorBlock(baseColors, entryProp.FindPropertyRelative("m_colors"));

        // SpriteState
        CopySpriteState(baseSpriteState, entryProp.FindPropertyRelative("m_spriteState"));
    }

    private static void CopyColorBlock(SerializedProperty src, SerializedProperty dst)
    {
        string[] colorFields =
        {
            "m_NormalColor", "m_HighlightedColor", "m_PressedColor",
            "m_SelectedColor", "m_DisabledColor"
        };
        foreach (var field in colorFields)
            dst.FindPropertyRelative(field).colorValue = src.FindPropertyRelative(field).colorValue;

        dst.FindPropertyRelative("m_ColorMultiplier").floatValue =
            src.FindPropertyRelative("m_ColorMultiplier").floatValue;
        dst.FindPropertyRelative("m_FadeDuration").floatValue =
            src.FindPropertyRelative("m_FadeDuration").floatValue;
    }

    private static void CopySpriteState(SerializedProperty src, SerializedProperty dst)
    {
        string[] spriteFields =
        {
            "m_HighlightedSprite", "m_PressedSprite",
            "m_SelectedSprite", "m_DisabledSprite"
        };
        foreach (var field in spriteFields)
            dst.FindPropertyRelative(field).objectReferenceValue =
                src.FindPropertyRelative(field).objectReferenceValue;
    }

    #endregion

    #region Draw Helpers

    private static void DrawColorTintFields(
        float x, ref float y, float w, float lineH, float spacing, SerializedProperty colorsProp)
    {
        DrawField(x, ref y, w, lineH, spacing, colorsProp.FindPropertyRelative("m_NormalColor"), "Normal Color");
        DrawField(x, ref y, w, lineH, spacing, colorsProp.FindPropertyRelative("m_HighlightedColor"), "Highlighted Color");
        DrawField(x, ref y, w, lineH, spacing, colorsProp.FindPropertyRelative("m_PressedColor"), "Pressed Color");
        DrawField(x, ref y, w, lineH, spacing, colorsProp.FindPropertyRelative("m_SelectedColor"), "Selected Color");
        DrawField(x, ref y, w, lineH, spacing, colorsProp.FindPropertyRelative("m_DisabledColor"), "Disabled Color");
        DrawField(x, ref y, w, lineH, spacing, colorsProp.FindPropertyRelative("m_ColorMultiplier"), "Color Multiplier");
        DrawField(x, ref y, w, lineH, spacing, colorsProp.FindPropertyRelative("m_FadeDuration"), "Fade Duration");
    }

    private static void DrawSpriteSwapFields(
        float x, ref float y, float w, float lineH, float spacing, SerializedProperty spriteStateProp)
    {
        DrawField(x, ref y, w, lineH, spacing, spriteStateProp.FindPropertyRelative("m_HighlightedSprite"), "Highlighted Sprite");
        DrawField(x, ref y, w, lineH, spacing, spriteStateProp.FindPropertyRelative("m_PressedSprite"), "Pressed Sprite");
        DrawField(x, ref y, w, lineH, spacing, spriteStateProp.FindPropertyRelative("m_SelectedSprite"), "Selected Sprite");
        DrawField(x, ref y, w, lineH, spacing, spriteStateProp.FindPropertyRelative("m_DisabledSprite"), "Disabled Sprite");
    }

    private static void DrawField(
        float x, ref float y, float w, float lineH, float spacing, SerializedProperty prop, string label)
    {
        var rect = new Rect(x, y, w, lineH);
        EditorGUI.PropertyField(rect, prop, new GUIContent(label));
        y += lineH + spacing;
    }

    #endregion
}
