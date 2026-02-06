using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HelpBoxAutoAttribute : PropertyAttribute
{
    public string Message { get; private set; }
    public HelpBoxMessageType MessageType { get; private set; }
    public Color TextColor { get; private set; }

    public HelpBoxAutoAttribute(string message,
        HelpBoxMessageType messageType = HelpBoxMessageType.Warning,
        string hexColor = "#FFFFFF")
    {
        Message = message;
        MessageType = messageType;
        TextColor = ColorUtility.TryParseHtmlString(hexColor, out var textColor) ? textColor : Color.white;
    }
}