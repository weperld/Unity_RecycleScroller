using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HelpBoxAutoAttribute))]
public class HelpBoxAutoDecoratorDrawer : DecoratorDrawer
{
    private HelpBoxAutoAttribute m_helpBoxAutoAttribute => (HelpBoxAutoAttribute)attribute;

    public override void OnGUI(Rect position)
    {
        // 커스텀 HelpBox 그리기
        EditorDrawerHelper.DrawCustomHelpBox(position, m_helpBoxAutoAttribute.Message, m_helpBoxAutoAttribute.MessageType.ToMessageType(), m_helpBoxAutoAttribute.TextColor);
    }

    public override float GetHeight()
    {
        // HelpBox의 높이 설정
        return EditorDrawerHelper.CalculateHelpBoxHeight(EditorDrawerHelper.HelpBoxTextStyle, EditorGUIUtility.currentViewWidth, m_helpBoxAutoAttribute.Message);
    }
}