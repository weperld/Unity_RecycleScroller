using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HelpBoxAutoAttribute))]
public class HelpBoxAutoDecoratorDrawer : DecoratorDrawer
{
    private HelpBoxAutoAttribute helpBoxAutoAttribute => (HelpBoxAutoAttribute)attribute;

    public override void OnGUI(Rect position)
    {
        // 커스텀 HelpBox 그리기
        EditorDrawerHelper.DrawCustomHelpBox(position, helpBoxAutoAttribute.Message, helpBoxAutoAttribute.MessageType.ToMessageType(), helpBoxAutoAttribute.TextColor);
    }

    public override float GetHeight()
    {
        // HelpBox의 높이 설정
        return EditorDrawerHelper.CalculateHelpBoxHeight(EditorDrawerHelper.HelpBoxTextStyle, EditorGUIUtility.currentViewWidth, helpBoxAutoAttribute.Message);
    }
}