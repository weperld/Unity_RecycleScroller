using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HorizontalLineAttribute : PropertyAttribute
{
    private const float defaultValue = 0.4f;

    private Color m_lineColor;
    public Color LineColor => m_lineColor;

    public HorizontalLineAttribute(float r, float g, float b, float a)
    {
        m_lineColor = new Color(r, g, b, a);
    }

    public HorizontalLineAttribute(float r, float g, float b)
    {
        m_lineColor = new Color(r, g, b);
    }

    public HorizontalLineAttribute() : this(defaultValue, defaultValue, defaultValue)
    {

    }
}