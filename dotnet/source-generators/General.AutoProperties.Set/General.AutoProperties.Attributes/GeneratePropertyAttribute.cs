using System;

namespace Plate.General.AutoProperties.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class GeneratePropertyAttribute : Attribute
{
    public string? PropertyName { get; set; }
    public PropertyAccessibility Accessibility { get; set; }
    public PropertyKind PropertyKind { get; set; }
    public bool IsRequired { get; set; }
    public bool IncludeInToString { get; set; }
    public bool ResolveInConstructor { get; set; }
    public bool NotifyPropertyChanged { get; set; }
}
