using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HelpBoxAttribute : PropertyAttribute
{
    public string Message { get; private set; }
    public eHelpBoxMessageType MessageType { get; private set; }
    public Color TextColor { get; private set; }
    public float Height { get; private set; }
    
    public HelpBoxAttribute(string message,
        eHelpBoxMessageType messageType = eHelpBoxMessageType.Warning,
        string hexColor = "#FFFFFF",
        float height = EditorDrawerHelper_ConstValues.DEFAULT_HELPBOX_HEIGHT)
    {
        Message = message;
        MessageType = messageType;
        TextColor = ColorUtility.TryParseHtmlString(hexColor, out var textColor) ? textColor : Color.white;
        Height = height;
    }
}