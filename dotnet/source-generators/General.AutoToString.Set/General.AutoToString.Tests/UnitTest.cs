// Copyright (c) GiantCroissant. All rights reserved.

namespace PlateShared.SCG.General.AutoToString.Tests;

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

namespace PlateShared.Game.Fake;

using PlateShared.SCG.General.AutoToString.Attributes;

[AutoToString]
public partial class SomeType01
{
    [AddToString]
    private readonly string _name;

    [AddToString]
    private readonly int _age;

    [AddToString]
    private readonly List<int> _values = new List<int>{ 1, 2, 3 };

    [AddToString]
    private readonly List<SomeType02> _someType02Values = new List<SomeType02>
    {
        new SomeType02(),
        new SomeType02()
    };
}

[AutoToString]
public partial class SomeType02
{
    [AddToString]
    private float _floatValue = 3.14f;

    [AddToString]
    private string _stringValue = "Hello, World!";
}
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(
            code,
            _testOutputHelper);
    }

    [Fact]
    public Task CheckAutoPropertyIncludeInToString()
    {
        const string code =
"""
using System;

namespace PlateShared.Game.Fake;

using PlateShared.SCG.General.AutoToString.Attributes;

// Minimal AutoProperties-like attributes for testing
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AutoPropertyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class GeneratePropertyAttribute : Attribute
{
    public bool IncludeInToString { get; set; }
}

[AutoToString]
[AutoProperty]
public partial class SomeTypeWithAutoProperties
{
    [GenerateProperty(IncludeInToString = true)]
    private readonly string _name;
}
""";

        return TestHelper.Verify(
            code,
            _testOutputHelper);
    }
}
