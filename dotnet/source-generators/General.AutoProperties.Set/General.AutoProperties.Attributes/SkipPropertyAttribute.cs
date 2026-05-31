using System;

namespace PlateShared.General.AutoProperties.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SkipPropertyAttribute : Attribute
{
}
