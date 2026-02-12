using System;
using System.Reflection;

public static class AttributeUtils
{
    public static bool HasAttribute<TTarget, TAttribute>(bool inherit = true)
        where TAttribute : Attribute
        => Attribute.IsDefined(typeof(TTarget), typeof(TAttribute), inherit);

    public static bool HasAttribute<TAttribute>(this Type targetType, bool inherit = true)
        where TAttribute : Attribute
        => Attribute.IsDefined(targetType, typeof(TAttribute), inherit);

    public static bool HasAttribute<TAttribute>(this MethodInfo targetMethod, out TAttribute attribute)
        where TAttribute : Attribute
    {
        attribute = targetMethod.GetCustomAttribute<TAttribute>();
        return attribute != null;
    }

    public static bool HasAttribute<TAttribute>(this MethodInfo targetMethod)
        where TAttribute : Attribute
        => targetMethod.HasAttribute<TAttribute>(out _);

    public static TAttribute GetAttribute<TTarget, TAttribute>(bool inherit = true)
        where TAttribute : Attribute
        => (TAttribute)Attribute.GetCustomAttribute(typeof(TTarget), typeof(TAttribute), inherit);
    
    public static TAttribute GetLastAttribute<TTarget, TAttribute>(bool inherit = true)
        where TAttribute : Attribute
    {
        var attributes = Attribute.GetCustomAttributes(typeof(TTarget), typeof(TAttribute), inherit);
        return attributes.Length > 0 ? (TAttribute)attributes[0] : null;
    }
}