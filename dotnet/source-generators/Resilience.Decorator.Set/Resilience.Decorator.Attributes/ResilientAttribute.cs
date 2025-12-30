using System;

namespace Plate.SCG.Resilience.Decorator.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ResilientAttribute : Attribute
{
    public ResilientAttribute()
    {
    }

    public ResilientAttribute(string policyName)
    {
        PolicyName = policyName;
    }

    public string? PolicyName { get; }
}
