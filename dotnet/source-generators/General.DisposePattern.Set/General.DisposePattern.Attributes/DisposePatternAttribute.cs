using System;

namespace PlateShared.SCG.General.DisposePattern.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DisposePatternAttribute : Attribute
{
}
