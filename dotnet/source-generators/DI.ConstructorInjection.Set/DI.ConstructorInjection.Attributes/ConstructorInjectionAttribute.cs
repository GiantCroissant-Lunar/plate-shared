using System;

namespace PlateShared.SCG.DI.ConstructorInjection.Attributes;

/// <summary>
/// This is marker attribute to apply the auto constructor to a class.
/// </summary>
/// <remarks>
/// Properties have to be initialized to default values in the order they are declared
/// in the constructor.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ConstructorInjectionAttribute : Attribute
{
}
