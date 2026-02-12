using CustomSerialization;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableKeyValuePair<,>), true)]
public partial class SerializableKeyValuePairDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var keyProperty = property.FindPropertyRelative("Key");
        var valueProperty = property.FindPropertyRelative("Value");
        
        var propertyType = GetPropertyType(valueProperty);
        var width = position.width / 2 - 20f;
        var valueRectOffset = propertyType switch
        {
            PropertyType.Default => -5f,
            _ => 7f
        };
        var keyWidth = width - 10f;
        var valueWidth = position.width - keyWidth - 10f;
        var keyRect = new Rect(position.x, position.y, keyWidth, EditorGUIUtility.singleLineHeight);
        var valueRect = new Rect(position.x + width + valueRectOffset, position.y, valueWidth - valueRectOffset, EditorGUIUtility.singleLineHeight);
        
        EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
        
        m_drawerStrategies[propertyType].DrawProperty(position, valueRect, valueProperty);
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var valueProperty = property.FindPropertyRelative("Value");
        var propertyType = GetPropertyType(valueProperty);
        var height = m_drawerStrategies[propertyType].GetPropertyHeight(valueProperty);
        
        return height;
    }
}