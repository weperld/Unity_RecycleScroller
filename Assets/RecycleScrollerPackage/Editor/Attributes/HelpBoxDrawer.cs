using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HelpBoxAttribute))]
public class HelpBoxDecoratorDrawer : DecoratorDrawer
{
    private HelpBoxAttribute helpBoxAttribute => (HelpBoxAttribute)attribute;
    
    public override void OnGUI(Rect position)
    {
        // 커스텀 HelpBox 그리기
        EditorDrawerHelper.DrawCustomHelpBox(position, helpBoxAttribute.Message, helpBoxAttribute.MessageType.ToMessageType(), helpBoxAttribute.TextColor);
    }
    
    public override float GetHeight()
    {
        // HelpBox의 높이 설정
        return helpBoxAttribute.Height;
    }
}