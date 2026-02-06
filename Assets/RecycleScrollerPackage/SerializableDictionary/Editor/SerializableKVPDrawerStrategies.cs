using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public partial class SerializableKeyValuePairDrawer
{
    private enum PropertyType
    {
        Default,
        Array,
        Generic
    }
    
    private readonly Dictionary<PropertyType, ISerializableKVPDrawerStrategy> m_drawerStrategies;
    public SerializableKeyValuePairDrawer()
    {
        m_drawerStrategies = new Dictionary<PropertyType, ISerializableKVPDrawerStrategy>
        {
            { PropertyType.Array, new ArrayPropertyDrawerStrategy() },
            { PropertyType.Generic, new GenericPropertyDrawerStrategy() },
            { PropertyType.Default, new DefaultPropertyDrawerStrategy() }
        };
    }
    
    private PropertyType GetPropertyType(SerializedProperty property)
    {
        // Array Type
        if (property.isArray && property.propertyType != SerializedPropertyType.String) return PropertyType.Array;
        // Generic Type
        if (property.propertyType == SerializedPropertyType.Generic) return PropertyType.Generic;
        // Default Type
        return PropertyType.Default;
    }
}

#region ISerializableKVPDrawerStrategy
public class ArrayPropertyDrawerStrategy : ISerializableKVPDrawerStrategy
{
    private ReorderableList reorderableList;
    
    public void DrawProperty(Rect position, Rect valueRect, SerializedProperty valueProperty)
    {
        InitializeReorderableList(valueProperty);
        
        valueProperty.isExpanded = EditorGUI.Foldout(valueRect, valueProperty.isExpanded, valueProperty.displayName, true);
        if (valueProperty.isExpanded == false) return;
        
        EditorGUI.indentLevel++;
        position.y += EditorGUIUtility.singleLineHeight + 2f;
        var listRect = new Rect(position.x, position.y, position.width, reorderableList.GetHeight());
        reorderableList.DoList(listRect);
        EditorGUI.indentLevel--;
    }

    public float GetPropertyHeight(SerializedProperty valueProperty)
    {
        InitializeReorderableList(valueProperty);
        
        if (valueProperty.isExpanded) return reorderableList.GetHeight() + EditorGUIUtility.singleLineHeight + 2f;
        return EditorGUIUtility.singleLineHeight;
    }
    
    private void InitializeReorderableList(SerializedProperty valueProperty)
    {
        reorderableList ??= new ReorderableList(valueProperty.serializedObject, valueProperty, true, false, true, true);
        reorderableList.serializedProperty = valueProperty;
        
        reorderableList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, valueProperty.displayName);
        };
        
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = valueProperty.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.x += 10f;     // 살짝 오른쪽으로 이동하여 들여쓰기
            rect.width -= 20f; // 여백을 주어 엘리먼트가 겹치지 않도록 설정
            
            var labelWidth = 70f; // 레이블의 너비 설정
            var fieldRect = new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height);
            var labelRect = new Rect(rect.x, rect.y - 4f, labelWidth - 5f, rect.height);
            
            EditorGUI.LabelField(labelRect, new GUIContent("Element " + index));
            EditorGUI.PropertyField(fieldRect, element, GUIContent.none, true);
        };
        
        reorderableList.elementHeightCallback = index =>
        {
            var element = valueProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + 4; // 요소 간 간격 추가
        };
        
        reorderableList.onAddCallback = _ =>
        {
            valueProperty.arraySize++;
        };
        
        reorderableList.onRemoveCallback = reorderableList =>
        {
            valueProperty.DeleteArrayElementAtIndex(reorderableList.index);
        };
    }
}

public class GenericPropertyDrawerStrategy : ISerializableKVPDrawerStrategy
{
    public void DrawProperty(Rect position, Rect valueRect, SerializedProperty valueProperty)
    {
        valueProperty.isExpanded = EditorGUI.Foldout(valueRect, valueProperty.isExpanded, new GUIContent(valueProperty.type), true);
        if (valueProperty.isExpanded is false) return;

        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        // 접기/펼치기에 따른 들여쓰기
        EditorGUI.indentLevel++;

        // 내부 필드 순회
        var valueField = valueProperty.Copy();
        var endProperty = valueProperty.GetEndProperty();
        valueField.NextVisible(true);

        while (SerializedProperty.EqualContents(valueField, endProperty) is false)
        {
            var fieldHeight = EditorGUI.GetPropertyHeight(valueField, null, true);
            var fieldRect = new Rect(position.x, position.y, position.width, fieldHeight);

            EditorGUI.PropertyField(fieldRect, valueField, new GUIContent(valueField.displayName), true);

            // 필드 하나 그린 뒤 높이 누적
            position.y += fieldHeight + EditorGUIUtility.standardVerticalSpacing;

            valueField.NextVisible(false);
        }

        EditorGUI.indentLevel--;
    }
    
    public float GetPropertyHeight(SerializedProperty valueProperty)
    {
        float height = EditorGUIUtility.singleLineHeight;
        
        if (valueProperty.isExpanded)
            height += EditorGUI.GetPropertyHeight(valueProperty, GUIContent.none, true) + 2 - EditorGUIUtility.singleLineHeight;
        
        return height;
    }
}

public class DefaultPropertyDrawerStrategy : ISerializableKVPDrawerStrategy
{
    public void DrawProperty(Rect position, Rect valueRect, SerializedProperty valueProperty)
    {
        EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
    }

    public float GetPropertyHeight(SerializedProperty valueProperty)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endregion