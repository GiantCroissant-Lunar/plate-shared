// Copyright (c) GiantCroissant. All rights reserved.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Plate.SCG.Resilience.Decorator;
using Plate.SCG.Resilience.Decorator.Attributes;
using Plate.Resilience;

namespace Plate.SCG.Resilience.Decorator.Tests;

public static class TestHelper
{
    public static (Compilation OutputCompilation, List<SyntaxTree> GeneratedTrees) Run(
        string sourceCode,
        ITestOutputHelper testOutputHelper)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(sourceCode, Encoding.UTF8));

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(SourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ResilientAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IResilienceService).Assembly.Location),
            });

        var compilation = CSharpCompilation.Create(
            assemblyName: "Plate.SCG.Resilience.Decorator.Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        foreach (var diagnostic in diagnostics)
        {
            testOutputHelper.WriteLine(diagnostic.ToString());
        }

        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(tree => tree != syntaxTree)
            .ToList();

        return (outputCompilation, generatedTrees);
    }
}
