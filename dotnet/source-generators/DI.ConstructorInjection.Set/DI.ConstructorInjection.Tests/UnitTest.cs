// Copyright (c) GiantCroissant. All rights reserved.

namespace PlateShared.SCG.DI.ConstructorInjection.Tests;

public class UnitTest : VerifyBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest(ITestOutputHelper testOutputHelper) : base()
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public Task CheckConstructorInjection()
    {
        // The source code to test
        const string code =
"""
using System;
using System.Collections.Generic;
using PlateShared.SCG.DI.ConstructorInjection.Attributes;

[ConstructorInjection]
public partial class SomeType01
{
    [ResolveInConstructor]
    private readonly string _name;

    [ResolveInConstructor]
    private readonly int _age;

    [ResolveInConstructor]
    private readonly List<int> _values;
}
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(
            code,
            _testOutputHelper);
    }

    [Fact]
    public Task CheckConstructorInjectionWithAutoPropertiesStyle()
    {
        const string code =
"""
using System;
using PlateShared.SCG.DI.ConstructorInjection.Attributes;

// Minimal AutoProperties-like attribute for testing
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class GeneratePropertyAttribute : Attribute
{
    public bool ResolveInConstructor { get; set; }
}

[ConstructorInjection]
public partial class SomeTypeWithAutoProperties
{
    [GenerateProperty(ResolveInConstructor = true)]
    private readonly string _name;

    [GenerateProperty(ResolveInConstructor = true)]
    private readonly int _age;
}
""";

        return TestHelper.Verify(
            code,
            _testOutputHelper);
    }
}
