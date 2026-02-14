using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

public static partial class EditorDrawerHelper
{
    private static GUIContent ToIcon(this MessageType messageType)
    {
        return messageType switch
        {
            MessageType.None => null,
            MessageType.Info => EditorGUIUtility.IconContent("console.infoicon"),
            MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon"),
            MessageType.Error => EditorGUIUtility.IconContent("console.erroricon"),
            _ => null
        };
    }

    public static MessageType ToMessageType(this HelpBoxMessageType messageType)
    {
        return messageType switch
        {
            HelpBoxMessageType.None => MessageType.None,
            HelpBoxMessageType.Info => MessageType.Info,
            HelpBoxMessageType.Warning => MessageType.Warning,
            HelpBoxMessageType.Error => MessageType.Error,
            _ => MessageType.None
        };
    }

    public static GUIStyle HelpBoxTextStyle => new(GUI.skin.label)
    {
        fontStyle = FontStyle.Bold,
        richText = true,
        wordWrap = true
    };
    public static float CalculateHelpBoxHeight(GUIStyle textStyle, float width, string message)
    {
        // 아이콘 크기와 텍스트 스타일 설정
        float iconSize = 20f;

        // 전체 너비 계산(에디터 가로 폭 - 좌우 여유 폭)
        // 아이콘 영역(약 20)과 여백(약간의 margin)을 고려해서 너비를 조정할 수 있음
        float contentWidth = width - 40f;
        // (취향껏 조정. 20 정도는 스크롤 바나 패딩 등을 고려한 여유 공간)

        // 텍스트 높이 계산
        float textHeight = textStyle.CalcHeight(new GUIContent(message), contentWidth);

        // 최종으로 쓸 높이 계산(아이콘 크기와 텍스트 높이 중 더 큰 값 + 상하 여백)
        // 아이콘이 크면 아이콘 크기에 맞춰야 하고, 텍스트가 더 크면 텍스트에 맞춰야 합니다.
        float finalHeight = Mathf.Max(iconSize, textHeight) + 10f;
        return finalHeight;
    }
    public static void DrawCustomHelpBox(Rect position, string message, MessageType messageType, Color color)
    {
        // 경고 아이콘 로드
        var iconContent = messageType.ToIcon();
        
        // 박스 스타일 설정
        var boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);
        boxStyle.margin = new RectOffset(4, 4, 4, 4);
        
        // 아이콘과 텍스트를 위한 공간 계산
        float iconSize = 20f; // 아이콘 크기 설정
        
        // 아이콘의 Y 좌표를 중앙으로 정렬
        float iconY = position.y + (position.height - iconSize) / 2;
        
        var iconRect = new Rect(position.x + 5, iconY, iconSize, iconSize);
        var textRect = new Rect(position.x + iconSize + 10, position.y, position.width - (iconSize + 15), position.height);
        
        // 박스 그리기
        GUI.Box(position, "", boxStyle);
        
        // 아이콘 그리기
        if (iconContent != null && iconContent.image != null)
        {
            GUI.DrawTexture(iconRect, iconContent.image, ScaleMode.ScaleToFit);
        }
        
        // 텍스트 스타일 설정
        GUIStyle textStyle = HelpBoxTextStyle;
        textStyle.normal.textColor = color; // 메시지 텍스트 색상 설정
        textStyle.hover.textColor = color;  // 마우스 오버 텍스트 색상 설정
        textStyle.alignment = TextAnchor.MiddleLeft;
        
        // 텍스트 그리기
        GUI.Label(textRect, message, textStyle);
    }
    public static void DrawCustomHelpBox(string message, MessageType messageType, Color color, float height)
    {
        // GUILayout을 사용하여 Rect를 내부에서 계산
        Rect position = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 20, height);
        
        // 기존 함수 호출
        DrawCustomHelpBox(position, message, messageType, color);
    }
    /// <summary>
    /// 높이 자동 조정(위 아래로 총 10의 패딩 추가)
    /// </summary>
    /// <param name="message"></param>
    /// <param name="messageType"></param>
    /// <param name="color"></param>
    public static void DrawCustomHelpBox(string message, MessageType messageType, Color color)
    {
        // 전체 너비 계산(에디터 가로 폭 - 좌우 여유 폭)
        // 아이콘 영역(약 20)과 여백(약간의 margin)을 고려해서 너비를 조정할 수 있음
        float contentWidth = EditorGUIUtility.currentViewWidth - 40f;

        // 최종으로 쓸 높이 계산(아이콘 크기와 텍스트 높이 중 더 큰 값 + 상하 여백)
        // 아이콘이 크면 아이콘 크기에 맞춰야 하고, 텍스트가 더 크면 텍스트에 맞춰야 합니다.
        float finalHeight = CalculateHelpBoxHeight(HelpBoxTextStyle, contentWidth, message);

        // 자동으로 Rect 생성
        Rect position = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 20f, finalHeight);

        // 기존에 작성했던 함수 재활용
        DrawCustomHelpBox(position, message, messageType, color);
    }
    
    public static bool HasCustomPropertyDrawer(FieldInfo fieldInfo)
    {
        var internalEditorUtilityType = typeof(PropertyDrawer).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
        var getDrawerTypeForTypeMethod = internalEditorUtilityType.GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic);
        var drawerType = getDrawerTypeForTypeMethod.Invoke(null, new object[] { fieldInfo.FieldType });
        
        return drawerType != null;
    }
    
    public static Type GetCustomPropertyDrawerType(FieldInfo fieldInfo)
    {
        var scriptAttributeUtilityType = typeof(PropertyDrawer).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
        var getDrawerTypeForTypeMethod = scriptAttributeUtilityType.GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic);
        
        var fieldType = fieldInfo.FieldType;
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
            fieldType = fieldType.GetGenericArguments()[0];
        
        bool isPropertyTypeAManagedReference = fieldInfo.GetCustomAttribute<SerializeReference>() != null;
        
        var parameters = new object[] { fieldType, isPropertyTypeAManagedReference };
        var drawerType = getDrawerTypeForTypeMethod.Invoke(null, parameters);
        return drawerType as Type;
    }
    
    public static bool TryGetPropertyHeightViaReflection(SerializedProperty property, GUIContent label, out float ret)
    {
        Type drawerType = GetCustomPropertyDrawerType(GetFieldInfoFromProperty(property));
        if (drawerType != null)
        {
            object drawerInstance = Activator.CreateInstance(drawerType);
            MethodInfo getHeightMethod = drawerType.GetMethod("GetPropertyHeight", BindingFlags.Instance | BindingFlags.Public);
            
            if (getHeightMethod != null)
            {
                ret = (float)getHeightMethod.Invoke(drawerInstance, new object[] { property, label });
                return true;
            }
        }
        
        ret = 0f;
        return false; // 기본 높이 반환
    }
    
    public static FieldInfo GetFieldInfoFromProperty(SerializedProperty prop)
    {
        object obj = prop.serializedObject.targetObject;
        Type objType = obj.GetType();
        FieldInfo field = null;
        string path = prop.propertyPath.Replace(".Array.data[", "[");
        string[] elements = path.Split('.');
        
        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                field = objType.GetField(elementName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                objType = field.FieldType;
                
                if (objType.IsArray)
                {
                    objType = objType.GetElementType(); // 배열 요소의 타입
                }
                else if (objType.IsGenericType)
                {
                    objType = objType.GetGenericArguments()[0]; // 제네릭 리스트의 요소 타입
                }
            }
            else
            {
                field = objType.GetField(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    objType = field.FieldType;
                }
            }
        }
        
        return field;
    }
    
    public static float CalculatePropertyHeight(SerializedProperty prop, GUIContent label, float baseHeight)
    {
        if (!prop.isExpanded) return baseHeight;
        
        var totalHeight = EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty iterator = prop.Copy(); // 현재 프로퍼티의 복사본을 만듭니다.
        
        iterator.NextVisible(true);
        while (true)
        {
            // 다음 보이는 필드로 이동합니다.
            if (!iterator.NextVisible(false) || iterator.propertyPath.StartsWith(prop.propertyPath + ".") == false)
                break;
            
            // 각 필드의 높이를 합산
            totalHeight += EditorGUI.GetPropertyHeight(iterator, null, true) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        return totalHeight;
    }

    public static void DrawDividerLine()
        => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    public static void DrawDividerLine(Color lineColor)
    {
        var style = new GUIStyle(GUI.skin.horizontalSlider);
        var prevColor = GUI.color;
        GUI.color = lineColor;
        EditorGUILayout.LabelField("", style);
        GUI.color = prevColor;
    }

    public static void DrawDividerLine(Color lineColor, Rect position)
    {
        // 위치 조정
        Rect lineRect = position;
        lineRect.height = EditorGUIUtility.standardVerticalSpacing;
        lineRect.y += EditorGUIUtility.singleLineHeight / 4f;

        EditorGUI.DrawRect(lineRect, lineColor);
    }

    #region Foldout Category Styles & Helpers

    // Big category box colors (alternating neutral grey tints)
    public static readonly Color BigBoxColorA = new(0.06f, 0.06f, 0.06f, 0.5f);
    public static readonly Color BigBoxColorB = new(0.14f, 0.14f, 0.14f, 0.5f);

    // Small category title colors (alternating green pastel)
    public const string SMALL_TITLE_COLOR_A = "#A8E6CF";
    public const string SMALL_TITLE_COLOR_B = "#C8E6A0";

    // Small category box colors (alternating green-grey)
    public static readonly Color SmallBoxColorA = new(0.20f, 0.28f, 0.22f, 0.45f);
    public static readonly Color SmallBoxColorB = new(0.26f, 0.30f, 0.20f, 0.45f);

    private static GUIStyle m_inspectorTitleStyle;
    public static GUIStyle InspectorTitleStyle
    {
        get
        {
            if (m_inspectorTitleStyle == null)
            {
                m_inspectorTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15
                };
            }
            return m_inspectorTitleStyle;
        }
    }

    private static GUIStyle m_bigFoldoutStyle;
    public static GUIStyle BigFoldoutStyle
    {
        get
        {
            if (m_bigFoldoutStyle == null)
            {
                m_bigFoldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    richText = true,
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
            }
            return m_bigFoldoutStyle;
        }
    }

    private static GUIStyle m_smallFoldoutStyle;
    public static GUIStyle SmallFoldoutStyle
    {
        get
        {
            if (m_smallFoldoutStyle == null)
            {
                m_smallFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    richText = true
                };
            }
            return m_smallFoldoutStyle;
        }
    }

    public static void DrawBigCategory(ref bool foldout, string title, string hexColor,
        Color boxColor, string subtitle, Action drawContent)
    {
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, boxColor);

        foldout = EditorGUILayout.Foldout(foldout,
            $"<color={hexColor}>{title}</color>", true, BigFoldoutStyle);
        if (foldout)
        {
            EditorGUILayout.LabelField($"↳ {subtitle}", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            drawContent();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    public static void DrawSmallCategory(ref bool foldout, string title, string hexColor,
        Color boxColor, Action drawContent)
    {
        var subRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(subRect, boxColor);

        var controlRect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(controlRect, boxColor);
        foldout = EditorGUI.Foldout(controlRect, foldout,
            $"<color={hexColor}>{title}</color>", true, SmallFoldoutStyle);

        if (foldout)
        {
            EditorGUI.indentLevel++;
            drawContent();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion
}