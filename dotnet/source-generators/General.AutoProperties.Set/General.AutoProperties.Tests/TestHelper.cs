using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using VerifyTests;
using VerifyXunit;
using Plate.SCG.General.AutoProperties;
using Plate.General.AutoProperties.Attributes;

namespace Plate.General.AutoProperties.Tests;

public static class TestHelper
{
    public static Task Verify(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8));

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(SourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AutoPropertyAttribute).Assembly.Location),
            });

        var compilation = CSharpCompilation.Create(
            assemblyName: "Plate.General.AutoProperties.Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(tree => tree != syntaxTree)
            .ToList();

        var generatedFiles = generatedTrees
            .ToDictionary(
                tree => Path.GetFileName(tree.FilePath) ?? $"generated_{generatedTrees.IndexOf(tree)}.cs",
                tree => tree.ToString());

        var verifySettings = new VerifySettings();
        verifySettings.UseDirectory("snapshots");

        return Verifier.Verify(generatedFiles, verifySettings);
    }
}
