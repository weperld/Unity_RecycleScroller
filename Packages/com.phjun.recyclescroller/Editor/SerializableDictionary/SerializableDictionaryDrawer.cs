using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using CustomSerialization;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    private ReorderableList m_reorderableList;

    private const float ClearButtonPadding = 5f;
    private const float ClearButtonWidth = 60f;
    private const string WrappingMessage =
        "This value type is not supported.\n"
        + "Please use a wrapper class to serialize this type.";

    // 새 항목 추가 UI 표시 여부
    private bool m_showAddEntryUI = false;

    // 새로 추가할 Key 임시 저장
    private object m_currentKeyInput;

    private Type KeyType => GetBaseGenericArguments()[0];
    private Type ValueType => GetBaseGenericArguments()[1];
    private bool IsNotSupportedValueType => ValueType.IsArray || ValueType.IsGenericType;

    #region OnGUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.indentLevel++;
        EditorGUI.BeginProperty(position, label, property);

        // 1) 먼저, 전체 높이를 구한다 (GetPropertyHeight 호출)
        float totalHeight = Internal_GetPropertyHeight(property);

        // 2) 그 높이에 맞춰 HelpBox 스타일 박스를 그린다
        var bigBoxRect = new Rect(position.x, position.y, position.width, totalHeight + 10f);
        GUI.Box(bigBoxRect, GUIContent.none, EditorStyles.helpBox);

        // 3) 박스 테두리와 내부 콘텐츠가 겹치지 않도록 소량의 여백(margin)을 준다
        float margin = 5f;
        position.x += margin;
        position.y += margin;
        position.width -= margin * 2f;
        // 높이는 OnGUI 로직 전체에서 조금씩 더할 것이므로, 여기선 건드리지 않음

        // ───────── 이제부터는 기존 코드 ─────────

        // "지원되지 않는 타입" 검사
        if (IsNotSupportedValueType)
        {
            DrawUnsupportedValueTypeError(position, property, label);
            EditorGUI.EndProperty();
            return;
        }

        // Foldout & Clear 버튼
        DrawFoldoutAndClearButton(position, property, label);

        // 펼쳐진 상태라면 추가 내용 그리기
        if (property.isExpanded)
        {
            float offsetY = position.y + EditorGUIUtility.singleLineHeight + 4;

            // "Add New Entry" 영역 (박스 등)
            offsetY = DrawAddEntryArea(position, property, offsetY);

            // ReorderableList
            offsetY = DrawList(position, property, offsetY);
        }

        EditorGUI.EndProperty();
        EditorGUI.indentLevel--;
    }

    #endregion

    #region (1) 지원되지 않는 타입 에러 표시

    private void DrawUnsupportedValueTypeError(Rect position, SerializedProperty property, GUIContent label)
    {
        position.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(position, label);

        var errorPosition = new Rect(
            position.x,
            position.y + EditorGUIUtility.singleLineHeight,
            position.width,
            EditorGUIUtility.singleLineHeight * 2
        );
        EditorGUI.HelpBox(errorPosition, WrappingMessage, MessageType.Error);
    }

    #endregion

    #region (2) Foldout & Clear 버튼

    private void DrawFoldoutAndClearButton(Rect position, SerializedProperty property, GUIContent label)
    {
        var foldOutRect = new Rect(
            position.x,
            position.y,
            position.width - ClearButtonWidth - ClearButtonPadding,
            EditorGUIUtility.singleLineHeight
        );
        property.isExpanded = EditorGUI.Foldout(foldOutRect, property.isExpanded, label, true);

        var clearButtonRect = new Rect(
            position.x + position.width - ClearButtonWidth,
            position.y,
            ClearButtonWidth,
            EditorGUIUtility.singleLineHeight
        );
        DrawClearButton(clearButtonRect, property);
    }

    #endregion

    #region (3) "Add Entry" 영역

    private float DrawAddEntryArea(Rect position, SerializedProperty property, float offsetY)
    {
        // "Add Entry" or "Hide Add Entry" 버튼 Rect
        var addEntryBtnRect = new Rect(
            position.x,
            offsetY,
            120,
            EditorGUIUtility.singleLineHeight
        );
        offsetY += EditorGUIUtility.singleLineHeight + 4;

        // m_showAddEntryUI에 따라 박스를 표시할지 결정
        if (m_showAddEntryUI)
        {
            // 박스 높이
            float boxHeight = EditorGUIUtility.singleLineHeight * 3 + 12;
            var innerDrawRect = new Rect(position.x, offsetY, position.width, boxHeight);

            // HelpBox 스타일 박스
            var boxRect = innerDrawRect;
            boxRect.y -= 12f;      // 살짝 위로 확장
            boxRect.height += 12f; // 박스 높이를 조금 크게
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            // 박스 내부 내용
            DrawAddNewEntryButton(addEntryBtnRect); // "Hide Add Entry" 버튼
            DrawNewEntryKeyUI(property, innerDrawRect);

            offsetY += boxHeight + 4;
        }
        else
        {
            // m_showAddEntryUI가 false면 "Add New Entry" 버튼만
            DrawAddNewEntryButton(addEntryBtnRect);
        }

        return offsetY;
    }

    private void DrawAddNewEntryButton(Rect position)
    {
        if (GUI.Button(position, m_showAddEntryUI ? "Hide Add Entry" : "Add New Entry"))
        {
            m_showAddEntryUI = !m_showAddEntryUI;
        }
    }

    // 박스 내부에서 Key 입력 + Confirm Add 버튼
    private void DrawNewEntryKeyUI(SerializedProperty property, Rect innerDrawRect)
    {
        float innerX = innerDrawRect.x + 8;
        float innerY = innerDrawRect.y + EditorGUIUtility.standardVerticalSpacing;
        float innerWidth = innerDrawRect.width - 16;

        // (A) 상단 라벨
        var titleRect = new Rect(
            innerX,
            innerY - 2f,
            innerWidth,
            EditorGUIUtility.singleLineHeight
        );
        var centerBoldLabel = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUI.LabelField(titleRect, "▸▸▸추가할 새 엔트리의 키 지정◂◂◂", centerBoldLabel);
        innerY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // (B) Key 입력 필드
        var keyFieldRect = new Rect(
            innerX,
            innerY,
            innerWidth,
            EditorGUIUtility.singleLineHeight
        );
        m_currentKeyInput = DrawSerializedPropertyTypeField(keyFieldRect, KeyType, m_currentKeyInput, "New Key");
        innerY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // (C) "Confirm Add" 버튼
        var confirmRect = new Rect(
            innerX,
            innerY,
            innerWidth,
            EditorGUIUtility.singleLineHeight
        );
        if (GUI.Button(confirmRect, "Confirm Add"))
        {
            AddCustomEntry(property, m_currentKeyInput);
        }
    }

    /// <summary>
    ///  특정 타입(fieldType)에 따라 적절한 EditorGUI.xxxField로
    ///  사용자 입력을 받아 object로 반환.
    /// </summary>
    private object DrawSerializedPropertyTypeField(Rect rect, Type fieldType, object currentValue, string label)
    {
        // int
        if (fieldType == typeof(int))
        {
            int prev = currentValue != null ? (int)currentValue : 0;
            return EditorGUI.IntField(rect, label, prev);
        }

        // float
        if (fieldType == typeof(float))
        {
            float prev = currentValue != null ? (float)currentValue : 0f;
            return EditorGUI.FloatField(rect, label, prev);
        }

        // bool
        if (fieldType == typeof(bool))
        {
            bool prev = currentValue != null ? (bool)currentValue : false;
            return EditorGUI.Toggle(rect, label, prev);
        }

        // string
        if (fieldType == typeof(string))
        {
            string prev = currentValue != null ? (string)currentValue : string.Empty;
            return EditorGUI.TextField(rect, label, prev);
        }

        // enum
        if (fieldType.IsEnum)
        {
            Enum prev = currentValue != null
                ? (Enum)currentValue
                : (Enum)Activator.CreateInstance(fieldType);
            return EditorGUI.EnumPopup(rect, label, prev);
        }

        // Color
        if (fieldType == typeof(Color))
        {
            Color prev = currentValue != null ? (Color)currentValue : Color.black;
            return EditorGUI.ColorField(rect, label, prev);
        }

        // Vector2
        if (fieldType == typeof(Vector2))
        {
            Vector2 prev = currentValue != null ? (Vector2)currentValue : Vector2.zero;
            return EditorGUI.Vector2Field(rect, label, prev);
        }

        // Vector3
        if (fieldType == typeof(Vector3))
        {
            Vector3 prev = currentValue != null ? (Vector3)currentValue : Vector3.zero;
            return EditorGUI.Vector3Field(rect, label, prev);
        }

        // Vector4
        if (fieldType == typeof(Vector4))
        {
            Vector4 prev = currentValue != null ? (Vector4)currentValue : Vector4.zero;
            return EditorGUI.Vector4Field(rect, label, prev);
        }

        // Rect
        if (fieldType == typeof(Rect))
        {
            Rect prev = currentValue != null ? (Rect)currentValue : new Rect(0, 0, 0, 0);
            return EditorGUI.RectField(rect, label, prev);
        }

        // AnimationCurve
        if (fieldType == typeof(AnimationCurve))
        {
            AnimationCurve prev = currentValue != null ? (AnimationCurve)currentValue : new AnimationCurve();
            return EditorGUI.CurveField(rect, label, prev);
        }

        // UnityEngine.Object (및 서브클래스)
        if (fieldType == typeof(UnityEngine.Object) || fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            UnityEngine.Object prev = currentValue as UnityEngine.Object;
            return EditorGUI.ObjectField(rect, label, prev, fieldType, false);
        }

        // Quaternion (직접 지원이 없어 Vector4로 처리)
        if (fieldType == typeof(Quaternion))
        {
            Quaternion prev = currentValue != null ? (Quaternion)currentValue : Quaternion.identity;
            Vector4 asVec = new Vector4(prev.x, prev.y, prev.z, prev.w);
            asVec = EditorGUI.Vector4Field(rect, label, asVec);
            return new Quaternion(asVec.x, asVec.y, asVec.z, asVec.w);
        }

        // 구조체(직렬화된)나 Generic 등 -> 간단히 미구현 처리
        if (fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum)
        {
            EditorGUI.LabelField(rect, label, $"Struct<{fieldType.Name}> (Not Implemented)");
            return currentValue;
        }

        // 그 외 "not supported"
        EditorGUI.LabelField(rect, label, $"Type<{fieldType.Name}> not supported");
        return currentValue;
    }

    /// <summary>
    ///  Confirm Add -> Key 중복 검사 후 새 항목 추가
    ///  Value는 SetDefaultValue로 초기화
    /// </summary>
    private void AddCustomEntry(SerializedProperty property, object keyObj)
    {
        var keyValuePairsProperty = property.FindPropertyRelative("m_keyValuePairs");

        // 1) 중복 키 검사
        bool isDuplicate = false;
        for (int i = 0; i < keyValuePairsProperty.arraySize; i++)
        {
            var element = keyValuePairsProperty.GetArrayElementAtIndex(i);
            var existingKeyProp = element.FindPropertyRelative("Key");
            if (AreKeysEqual(existingKeyProp, keyObj))
            {
                Debug.LogWarning($"Duplicate Key \"{keyObj}\". Entry not added.");
                isDuplicate = true;
                break;
            }
        }

        if (isDuplicate)
            return;

        // 2) 새 항목 추가
        int newIndex = keyValuePairsProperty.arraySize;
        keyValuePairsProperty.arraySize++;

        var newElement = keyValuePairsProperty.GetArrayElementAtIndex(newIndex);
        var newKeyProp = newElement.FindPropertyRelative("Key");
        var newValueProp = newElement.FindPropertyRelative("Value");

        // Key 지정
        ApplyObjectToSerializedProperty(newKeyProp, keyObj);

        // Value는 기본값
        SetDefaultValue(newValueProp);

        property.serializedObject.ApplyModifiedProperties();
        Debug.Log($"New Entry Added: Key = {keyObj} (Value=Default)");
    }

    #endregion

    #region (4) ReorderableList

    private float DrawList(Rect position, SerializedProperty property, float offsetY)
    {
        InitializeReorderableList(property);

        var listRect = new Rect(
            position.x,
            offsetY,
            position.width,
            m_reorderableList.GetHeight()
        );
        m_reorderableList.DoList(listRect);

        offsetY += m_reorderableList.GetHeight();
        return offsetY;
    }

    private void InitializeReorderableList(SerializedProperty property)
    {
        var keyValuePairsProperty = property.FindPropertyRelative("m_keyValuePairs");

        if (m_reorderableList == null)
        {
            m_reorderableList = new ReorderableList(
                keyValuePairsProperty.serializedObject,
                keyValuePairsProperty,
                true,  // draggable
                true,  // show header
                false, // hide + button
                true   // show remove button
            );

            m_reorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, $"Key: {KeyType.Name} / Value: {ValueType.Name}");
            };

            m_reorderableList.drawElementCallback = (rect, index, _, _) =>
            {
                var element = keyValuePairsProperty.GetArrayElementAtIndex(index);
                rect.y += 2;

                var kvpDrawer = new SerializableKeyValuePairDrawer();
                float elementHeight = kvpDrawer.GetPropertyHeight(element, GUIContent.none);
                var keyValuePairRect = new Rect(rect.x, rect.y, rect.width, elementHeight);

                kvpDrawer.OnGUI(keyValuePairRect, element, GUIContent.none);
            };

            m_reorderableList.elementHeightCallback = index =>
            {
                var element = keyValuePairsProperty.GetArrayElementAtIndex(index);
                var kvpDrawer = new SerializableKeyValuePairDrawer();
                return kvpDrawer.GetPropertyHeight(element, GUIContent.none) + 8f;
            };

            m_reorderableList.onAddCallback = _ => { };
            m_reorderableList.onRemoveCallback = list =>
            {
                keyValuePairsProperty.DeleteArrayElementAtIndex(list.index);
            };
        }
        else
        {
            m_reorderableList.serializedProperty = keyValuePairsProperty;
        }
    }

    #endregion

    #region Key Comparison

    private bool AreKeysEqual(SerializedProperty keyProp, object keyObj)
    {
        if (keyObj == null)
        {
            if (keyProp.propertyType == SerializedPropertyType.ObjectReference)
                return (keyProp.objectReferenceValue == null);
            return false;
        }

        switch (keyProp.propertyType)
        {
            case SerializedPropertyType.Integer:
                return keyProp.intValue == Convert.ToInt32(keyObj);
            case SerializedPropertyType.Boolean:
                return keyProp.boolValue == Convert.ToBoolean(keyObj);
            case SerializedPropertyType.Float:
                return Mathf.Approximately(keyProp.floatValue, Convert.ToSingle(keyObj));
            case SerializedPropertyType.String:
                return keyProp.stringValue == (string)keyObj;
            case SerializedPropertyType.ObjectReference:
                return keyProp.objectReferenceValue == (UnityEngine.Object)keyObj;
            case SerializedPropertyType.Enum:
                return keyProp.enumValueIndex == Convert.ToInt32(keyObj);
        }

        return false;
    }

    #endregion

    #region Default Value

    private void SetDefaultValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                prop.intValue = 0; break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = false; break;
            case SerializedPropertyType.Float:
                prop.floatValue = 0f; break;
            case SerializedPropertyType.String:
                prop.stringValue = string.Empty; break;
            case SerializedPropertyType.Color:
                prop.colorValue = Color.black; break;
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = null; break;
            case SerializedPropertyType.LayerMask:
                prop.intValue = 0; break;
            case SerializedPropertyType.Enum:
                prop.enumValueIndex = 0; break;
            case SerializedPropertyType.Vector2:
                prop.vector2Value = Vector2.zero; break;
            case SerializedPropertyType.Vector3:
                prop.vector3Value = Vector3.zero; break;
            case SerializedPropertyType.Vector4:
                prop.vector4Value = Vector4.zero; break;
            case SerializedPropertyType.Rect:
                prop.rectValue = new Rect(0, 0, 0, 0); break;
            case SerializedPropertyType.ArraySize:
                prop.intValue = 0; break;
            case SerializedPropertyType.Character:
                prop.intValue = 0; break;
            case SerializedPropertyType.AnimationCurve:
                prop.animationCurveValue = new AnimationCurve(); break;
            case SerializedPropertyType.Bounds:
                prop.boundsValue = new Bounds(Vector3.zero, Vector3.zero); break;
            case SerializedPropertyType.Gradient:
                break;
            case SerializedPropertyType.Quaternion:
                prop.quaternionValue = Quaternion.identity; break;
            case SerializedPropertyType.ExposedReference:
                prop.exposedReferenceValue = null; break;
            case SerializedPropertyType.FixedBufferSize:
                prop.intValue = 0; break;
            case SerializedPropertyType.Vector2Int:
                prop.vector2IntValue = Vector2Int.zero; break;
            case SerializedPropertyType.Vector3Int:
                prop.vector3IntValue = Vector3Int.zero; break;
            case SerializedPropertyType.RectInt:
                prop.rectIntValue = new RectInt(0, 0, 0, 0); break;
            case SerializedPropertyType.BoundsInt:
                prop.boundsIntValue = new BoundsInt(); break;
            case SerializedPropertyType.ManagedReference:
                prop.managedReferenceValue = null; break;
            case SerializedPropertyType.Generic:
                ResetGenericProperty(prop);
                break;
        }
    }

    private void ResetGenericProperty(SerializedProperty prop)
    {
        var copy = prop.Copy();
        var end = copy.GetEndProperty();

        if (!copy.NextVisible(true))
            return;

        while (!SerializedProperty.EqualContents(copy, end))
        {
            SetDefaultValue(copy);
            if (!copy.NextVisible(false))
                break;
        }
    }

    #endregion

    #region Apply Key -> SerializedProperty

    private void ApplyObjectToSerializedProperty(SerializedProperty prop, object val)
    {
        if (val == null)
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                prop.objectReferenceValue = null;
            }
            else if (prop.propertyType == SerializedPropertyType.Integer ||
                prop.propertyType == SerializedPropertyType.Float)
            {
                prop.intValue = 0;
            }

            return;
        }

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                prop.intValue = Convert.ToInt32(val); break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = Convert.ToBoolean(val); break;
            case SerializedPropertyType.Float:
                prop.floatValue = Convert.ToSingle(val); break;
            case SerializedPropertyType.String:
                prop.stringValue = (string)val; break;
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = val as UnityEngine.Object; break;
            case SerializedPropertyType.Color:
                prop.colorValue = (Color)val; break;
            case SerializedPropertyType.Enum:
                prop.enumValueIndex = Convert.ToInt32(val); break;
            case SerializedPropertyType.Vector2:
                prop.vector2Value = (Vector2)val; break;
            case SerializedPropertyType.Vector3:
                prop.vector3Value = (Vector3)val; break;
            case SerializedPropertyType.Vector4:
                prop.vector4Value = (Vector4)val; break;
            case SerializedPropertyType.Vector2Int:
                prop.vector2IntValue = (Vector2Int)val; break;
            case SerializedPropertyType.Vector3Int:
                prop.vector3IntValue = (Vector3Int)val; break;
            case SerializedPropertyType.Rect:
                prop.rectValue = (Rect)val; break;
            case SerializedPropertyType.RectInt:
                prop.rectIntValue = (RectInt)val; break;
            case SerializedPropertyType.Bounds:
                prop.boundsValue = (Bounds)val; break;
            case SerializedPropertyType.BoundsInt:
                prop.boundsIntValue = (BoundsInt)val; break;
            case SerializedPropertyType.AnimationCurve:
                prop.animationCurveValue = (AnimationCurve)val; break;
            case SerializedPropertyType.Quaternion:
                prop.quaternionValue = (Quaternion)val; break;
            case SerializedPropertyType.Gradient:
                break;
        }
    }

    #endregion

    #region Clear Button

    private void DrawClearButton(Rect position, SerializedProperty property)
    {
        var keyValuePairs = property.FindPropertyRelative("m_keyValuePairs");
        bool isEmpty = keyValuePairs.arraySize == 0;
        GUI.enabled = !isEmpty;
        var countStr = keyValuePairs.arraySize.ToString();
        var buttonName = isEmpty ? "Empty" : $"Clear ({countStr})";

        if (GUI.Button(position, buttonName))
        {
            Undo.RecordObject(property.serializedObject.targetObject, "Clear SerializableDictionary");
            keyValuePairs.arraySize = 0;
            property.serializedObject.ApplyModifiedProperties();
            m_reorderableList = null;
        }

        GUI.enabled = true;
    }

    #endregion

    #region GetPropertyHeight

    private float Internal_GetPropertyHeight(SerializedProperty property)
    {
        // 1) 지원 불가 타입이면 HelpBox 높이
        if (IsNotSupportedValueType)
            return EditorGUIUtility.singleLineHeight * 3;

        // 2) 접혀 있으면 foldout 한 줄만
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        // 펼친 상태
        InitializeReorderableList(property);

        float lineH = EditorGUIUtility.singleLineHeight + 4;
        float totalH = lineH; // foldout 포함

        // Add Entry 버튼
        totalH += (EditorGUIUtility.singleLineHeight + 4);

        // 박스 영역
        if (m_showAddEntryUI)
        {
            float boxH = EditorGUIUtility.singleLineHeight * 3 + 12;
            totalH += boxH + 4;
        }

        // ReorderableList
        totalH += m_reorderableList.GetHeight();

        return totalH;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return Internal_GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing * 5f;
    }

    #endregion

    #region Util

    private Type[] GetBaseGenericArguments()
    {
        var fieldType = fieldInfo.FieldType;
        if (IsSerializableDictionaryType(fieldType))
            return fieldType.GetGenericArguments();

        var baseType = fieldType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (IsSerializableDictionaryType(baseType))
                return baseType.GetGenericArguments();
            baseType = baseType.BaseType;
        }

        return null;
    }

    private static bool IsSerializableDictionaryType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SerializableDictionary<,>);

    #endregion
}