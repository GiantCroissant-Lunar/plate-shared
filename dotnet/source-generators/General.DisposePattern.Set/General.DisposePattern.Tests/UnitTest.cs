// Copyright (c) GiantCroissant. All rights reserved.

namespace Plate.SCG.General.DisposePattern.Tests;

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

namespace Plate.Game.Fake;

using Plate.SCG.General.DisposePattern.Attributes;

public class Person : IDisposable
{
    private string _name = "me";

    public void Dispose()
    {
        _name = null;
    }
}

[DisposePattern]
public sealed partial class SomeType01
{
    [ToBeDisposed]
    private readonly Person _person;

    private readonly int _age;

    // This field will not be disposed
    [ToBeDisposed]
    private readonly List<int> _values;

    // This field will not be disposed
    [ToBeDisposed]
    private List<string>? _nullableValues;
}
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(
            code,
            _testOutputHelper);
    }
}
