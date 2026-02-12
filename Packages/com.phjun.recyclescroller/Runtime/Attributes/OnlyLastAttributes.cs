using System;
using System.Collections.Generic;
using System.Reflection;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public abstract class OnlyLastAttribute : Attribute
{
    public abstract void Action<T>(Action<T> action, params T[] targets);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class OnlyLastNoneAttribute : OnlyLastAttribute
{
    public override void Action<T>(Action<T> action, params T[] targets)
    {
        if (targets is null or { Length: 0 } || action is null) return;
        
        foreach (var target in targets)
            action(target);
    }
}

/// <summary>
/// 파생 클래스 포함 마지막 것만 사용
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class OnlyLastIncludingDerivedAttribute : OnlyLastAttribute
{
    public override void Action<T>(Action<T> action, params T[] targets)
    {
        if (targets is null or { Length: 0 } || action is null) return;
        
        action(targets[^1]);
    }
}

/// <summary>
/// 파생 클래스에 대해 파생 타입별로 나눈 후 각 타입별 마지막 것만 사용
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class OnlyLastPerDerivedAttribute : OnlyLastAttribute
{
    public override void Action<T>(Action<T> action, params T[] targets)
    {
        if (targets is null or { Length: 0 } || action is null) return;
        
        Dictionary<Type, T> lastPerDerived = new();
        foreach (var target in targets)
        {
            var targetType = target.GetType();
            lastPerDerived.TryAdd(targetType, default);
            lastPerDerived[targetType] = target;
        }
        
        foreach (var target in lastPerDerived.Values)
            action(target);
    }
}

public static class Extensions_OnlyLastAttribute
{
    private static OnlyLastAttribute GetOnlyLastAttribute<T>()
    {
        return AttributeUtils.GetLastAttribute<T, OnlyLastAttribute>() ?? new OnlyLastNoneAttribute();
    }
    
    public static void ActionForOnlyLastAttribute<T>(this T[] targets, Action<T> action)
    {
        if (targets is null or { Length: 0 } || action is null) return;
        
        var attr = GetOnlyLastAttribute<T>();
        attr.Action(action, targets);
    }
}