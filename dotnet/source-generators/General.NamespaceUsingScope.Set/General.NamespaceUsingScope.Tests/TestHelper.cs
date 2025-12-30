using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Plate.SCG.General.NamespaceUsingScope;

namespace Plate.General.NamespaceUsingScope.Tests;

public static class TestHelper
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string configJson)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new NamespaceUsingScopeAnalyzer();

        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("NamespaceUsingScope.config.json", configJson));

        var analyzerOptions = new AnalyzerOptions(additionalTexts);
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);

        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, analyzerOptions);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _sourceText;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _sourceText = SourceText.From(text, System.Text.Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _sourceText;
    }
}
