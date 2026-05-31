// Copyright (c) GiantCroissant. All rights reserved.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using PlateShared.SCG.DI.ConstructorInjection;
using PlateShared.SCG.DI.ConstructorInjection.Attributes;

namespace PlateShared.SCG.DI.ConstructorInjection.Tests;

public static class TestHelper
{
    public static async Task Verify(
        string sourceCode,
        ITestOutputHelper testOutputHelper)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(sourceCode, Encoding.UTF8));

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(SourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ResolveInConstructorAttribute).Assembly.Location),
            });

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "PlateShared.SCG.DI.ConstructorInjection.Tests",
            syntaxTrees: new[] {syntaxTree},
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create an instance of our incremental source generator
        var generator = new SourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generator driver
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

        // Verify that only one file was generated
        Assert.Single(generatedTrees);

        // Get the generated code
        var generatedCode = generatedTrees[^1].ToString();

        var verifySettings = new VerifySettings();
        verifySettings.UseDirectory("snapshots");

        await Verifier.Verify(generatedCode, verifySettings);
    }
}
