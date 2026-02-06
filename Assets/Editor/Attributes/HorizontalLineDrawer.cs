using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HorizontalLineAttribute))]
public class HorizontalLineDrawer : DecoratorDrawer
{
    public override void OnGUI(Rect position)
    {
        // HorizontalLineAttribute에서 지정한 색상 가져오기
        HorizontalLineAttribute lineAttribute = (HorizontalLineAttribute)attribute;
        EditorDrawerHelper.DrawDividerLine(lineAttribute.LineColor, position);
    }

    public override float GetHeight()
    {
        return EditorGUIUtility.singleLineHeight / 2f + EditorGUIUtility.standardVerticalSpacing;
    }
}