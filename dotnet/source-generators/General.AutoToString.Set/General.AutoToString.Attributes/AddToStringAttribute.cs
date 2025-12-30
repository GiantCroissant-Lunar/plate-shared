using System;

namespace Plate.SCG.General.AutoToString.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class AddToStringAttribute : Attribute
{
}
