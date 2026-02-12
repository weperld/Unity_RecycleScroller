using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RecycleScroll.BoolVector2))]
public class BoolVector2Drawer : PropertyDrawer
{
    // BoolVector2의 width와 height 필드를 Vector2처럼 한 줄에 표기하기 위한 커스텀 프로퍼티 드로어
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        // 레이블을 표시
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // width와 height 프로퍼티의 SerializedProperty를 가져옴
        SerializedProperty width = property.FindPropertyRelative("width");
        SerializedProperty height = property.FindPropertyRelative("height");

        // width와 height 프로퍼티를 직접 컨트롤
        Rect rect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
        EditorGUIUtility.labelWidth = 45f;
        EditorGUI.PropertyField(rect, width, new GUIContent("Width"));

        rect.x += position.width * 0.5f;
        EditorGUI.PropertyField(rect, height, new GUIContent("Height"));
        EditorGUI.EndProperty();
    }
}
