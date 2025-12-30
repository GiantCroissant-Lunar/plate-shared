// Copyright (c) GiantCroissant. All rights reserved.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace Plate.SCG.Resilience.Decorator.Tests;

public class UnitTest : VerifyBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest(ITestOutputHelper testOutputHelper) : base()
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public Task GeneratesDecoratorAndCompiles()
    {
        const string code =
"""
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

using Plate.SCG.Resilience.Decorator.Attributes;

[Resilient("http")]
public interface IFoo
{
    Task PingAsync(CancellationToken ct = default);

    Task<int> AddAsync(int a, int b, CancellationToken ct = default);

    int AddSync(int a, int b);
}
""";

        var (outputCompilation, generatedTrees) = TestHelper.Run(code, _testOutputHelper);

        generatedTrees.Should().NotBeEmpty();

        var errors = outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        errors.Should().BeEmpty();

        var generatedCode = generatedTrees.Single().ToString();
        generatedCode.Should().Contain("ResilienceDecorator");
        generatedCode.Should().Contain("ResilienceServiceExtensions.ExecuteAsync");
        generatedCode.Should().Contain("\"http\"");

        return Task.CompletedTask;
    }
}
