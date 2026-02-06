using UnityEditor;
using UnityEngine;

public interface ISerializableKVPDrawerStrategy
{
    void DrawProperty(Rect position, Rect valueRect, SerializedProperty valueProperty);
    float GetPropertyHeight(SerializedProperty valueProperty);
}