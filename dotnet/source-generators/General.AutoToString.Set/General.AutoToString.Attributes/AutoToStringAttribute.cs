using System;

namespace PlateShared.SCG.General.AutoToString.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AutoToStringAttribute : Attribute
{
}
