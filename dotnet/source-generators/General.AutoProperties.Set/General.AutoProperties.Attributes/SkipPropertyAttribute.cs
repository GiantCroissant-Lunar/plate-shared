using System;

namespace Plate.General.AutoProperties.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SkipPropertyAttribute : Attribute
{
}
