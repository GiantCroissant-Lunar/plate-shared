using System;

namespace PlateShared.SCG.General.DisposePattern.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ToBeDisposedAttribute : Attribute
{
}
