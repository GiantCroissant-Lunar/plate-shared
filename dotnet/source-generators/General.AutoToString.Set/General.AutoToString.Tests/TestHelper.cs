// Copyright (c) GiantCroissant. All rights reserved.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using PlateShared.SCG.General.AutoToString;
using PlateShared.SCG.General.AutoToString.Attributes;

namespace PlateShared.SCG.General.AutoToString.Tests;

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
                MetadataReference.CreateFromFile(typeof(AutoToStringAttribute).Assembly.Location),
            });

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "PlateShared.SCG.General.AutoToString.Tests",
            syntaxTrees: new[] { syntaxTree },
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

        // Collect all generated files into a dictionary: filename => contents
        var generatedFiles = generatedTrees
            .ToDictionary(
                tree => Path.GetFileName(tree.FilePath) ?? $"generated_{generatedTrees.IndexOf(tree)}.cs",
                tree => tree.ToString());

        var verifySettings = new VerifySettings();
        verifySettings.UseDirectory("snapshots");

        await Verifier.Verify(generatedFiles, verifySettings);
    }
}
