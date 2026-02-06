using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class ColoredHeaderAttribute : PropertyAttribute
{
    public string Header { get; private set; }
    public Color TextColor { get; private set; }
    
    public ColoredHeaderAttribute(string header, float r, float g, float b)
    {
        Header = header;
        TextColor = new Color(r, g, b);
    }
    
    public ColoredHeaderAttribute(string header, string hexColor)
    {
        Header = header;
        TextColor = ColorUtility.TryParseHtmlString(hexColor, out var textColor) ? textColor : Color.white;
    }

    public ColoredHeaderAttribute(string header) : this(header, ColorHexTemplate.CT_HEX_888888) { }
}