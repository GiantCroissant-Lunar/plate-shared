using System;

namespace PlateShared.General.AutoProperties.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AutoPropertyAttribute : Attribute
{
    public PropertyAccessibility DefaultAccessibility { get; set; } = PropertyAccessibility.Public;
    public PropertyKind DefaultPropertyKind { get; set; } = PropertyKind.GetterSetter;
    public bool GenerateForAllFields { get; set; }
    public string FieldPrefix { get; set; } = "_";
}

public enum PropertyAccessibility
{
    Public,
    Internal,
    Protected,
    Private
}

public enum PropertyKind
{
    GetterSetter,
    GetterOnly,
    GetterPrivateSetter,
    GetterProtectedSetter,
    GetterInternalSetter,
    GetterInitOnly
}
