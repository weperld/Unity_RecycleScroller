using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ColoredHeaderAttribute))]
public class ColoredHeaderDrawer : DecoratorDrawer
{
    public override void OnGUI(Rect position)
    {
        var coloredHeader = (ColoredHeaderAttribute)attribute;
        
        // 텍스트 스타일 설정
        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = coloredHeader.TextColor },
            alignment = TextAnchor.LowerLeft // 텍스트를 하단 왼쪽 정렬로 설정
        };
        
        // 텍스트가 하단에 그려지도록 Y 좌표 조정
        var adjustedYPosition = position.y + (position.height - EditorGUIUtility.singleLineHeight);
        var textRect = new Rect(position.x, adjustedYPosition, position.width, EditorGUIUtility.singleLineHeight);
        
        // 헤더 그리기
        EditorGUI.LabelField(textRect, coloredHeader.Header, headerStyle);
    }
    
    public override float GetHeight()
    {
        // 일반적인 Header와 유사하게 위에 간격을 추가합니다.
        return EditorGUIUtility.singleLineHeight + 10f;
    }
}