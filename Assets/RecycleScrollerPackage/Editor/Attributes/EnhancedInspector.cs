using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

// 공통 기능을 담당하는 베이스 클래스 (추상 클래스)
public abstract class EnhancedInspectorBase : Editor
{
    // 타입별로 propertyPath -> label 텍스트 캐시
    private static readonly Dictionary<System.Type, Dictionary<string, string>> s_LabelCache = new();
    private static readonly GUIContent s_TempLabel = new GUIContent(); // GC 최소화를 위한 재사용 버퍼

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHelpBoxes();

        serializedObject.ApplyModifiedProperties();
    }

    protected void DrawHelpBoxes()
    {
        var targetType = target.GetType();

        if (targetType.GetCustomAttributes(typeof(HelpBoxAttribute), true) is HelpBoxAttribute[] { Length: > 0 } helpBoxAttributes)
        {
            // 부모 클래스의 어트리뷰트를 먼저 그림
            for (int index = helpBoxAttributes.Length - 1; index >= 0; index--)
            {
                HelpBoxAttribute helpBoxAttribute = helpBoxAttributes[index];
                EditorDrawerHelper.DrawCustomHelpBox(
                    EditorGUILayout.GetControlRect(false, helpBoxAttribute.Height),
                    helpBoxAttribute.Message,
                    helpBoxAttribute.MessageType.ToMessageType(),
                    helpBoxAttribute.TextColor);
            }
        }

        if (targetType.GetCustomAttributes(typeof(HelpBoxAutoAttribute), true) is HelpBoxAutoAttribute[] { Length: > 0 } helpBoxAutoAttributes)
        {
            // 부모 클래스의 어트리뷰트를 먼저 그림
            for (int index = helpBoxAutoAttributes.Length - 1; index >= 0; index--)
            {
                HelpBoxAutoAttribute helpBoxAutoAttribute = helpBoxAutoAttributes[index];
                EditorDrawerHelper.DrawCustomHelpBox(
                    helpBoxAutoAttribute.Message,
                    helpBoxAutoAttribute.MessageType.ToMessageType(),
                    helpBoxAutoAttribute.TextColor);
            }
        }
    }
}

// MonoBehaviour용 HelpBoxEditor
[CustomEditor(typeof(MonoBehaviour), true)]
public class EnhancedInspectorMonoBehaviour : EnhancedInspectorBase
{
    // 베이스 클래스의 기능을 상속받아 사용
}

// ScriptableObject용 HelpBoxEditor
[CustomEditor(typeof(ScriptableObject), true)]
public class EnhancedInspectorScriptableObject : EnhancedInspectorBase
{
    // 베이스 클래스의 기능을 상속받아 사용
}