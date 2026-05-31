using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlateShared.SCG.General.NamespaceUsingScope;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamespaceUsingScopeAnalyzer : DiagnosticAnalyzer
{
    public const string UsingShouldBeInsideNamespaceId = "NSUSG001";
    public const string UsingShouldBeFileScopedId = "NSUSG002";
    public const string UsingOrderInvalidId = "NSUSG003";
    public const string FileScopedInsidePreferenceId = "NSUSG004";

    private static readonly DiagnosticDescriptor UsingShouldBeInsideNamespaceRule = new(
        UsingShouldBeInsideNamespaceId,
        title: "Using directive should be inside namespace",
        messageFormat: "Using for '{0}' should be placed inside the namespace declaration",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);


    private static readonly DiagnosticDescriptor UsingShouldBeFileScopedRule = new(
        UsingShouldBeFileScopedId,
        title: "Using directive should be file-scoped",
        messageFormat: "Using for '{0}' should be placed at the top of the file, outside the namespace",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UsingOrderInvalidRule = new(
        UsingOrderInvalidId,
        title: "Using directives are not ordered according to namespace scope rules",
        messageFormat: "Using directives should be ordered so that outside-namespace usings come first, then inside-namespace usings",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FileScopedInsidePreferenceRule = new(
        FileScopedInsidePreferenceId,
        title: "Using directive prefers inside-namespace placement in file-scoped namespace",
        messageFormat: "Using for '{0}' is configured to be inside the namespace, but this file uses a file-scoped namespace. Consider converting to a block-scoped namespace if strict scoping is required.",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            UsingShouldBeInsideNamespaceRule,
            UsingShouldBeFileScopedRule,
            UsingOrderInvalidRule,
            FileScopedInsidePreferenceRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(StartAnalysis);
    }

    private static void StartAnalysis(CompilationStartAnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            AnalyzeCompilationUnit,
            SyntaxKind.CompilationUnit);
    }

    private static void AnalyzeCompilationUnit(
        SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not CompilationUnitSyntax compilationUnit)
        {
            return;
        }

        var config = NamespaceUsingScopeConfig.Load(context.Options, compilationUnit.SyntaxTree);

        if (config.InsideNamespacePrefixes.IsDefaultOrEmpty)
        {
            // No prefixes configured; analyzer is effectively disabled for this file.
            return;
        }

        var fileScopedNamespace = compilationUnit.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNamespace is not null)
        {
            AnalyzeFileScopedNamespace(context, compilationUnit, config);
            return;
        }

        // Block-scoped namespaces: enforce actual inside/outside placement
        var namespaceDeclaration = compilationUnit.Members
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (namespaceDeclaration is null)
        {
            return;
        }

        AnalyzeBlockScopedNamespace(context, compilationUnit, namespaceDeclaration, config);
    }

    private static void AnalyzeBlockScopedNamespace(
        SyntaxNodeAnalysisContext context,
        CompilationUnitSyntax compilationUnit,
        NamespaceDeclarationSyntax namespaceDeclaration,
        NamespaceUsingScopeConfig config)
    {
        foreach (var usingDirective in compilationUnit.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (usingDirective.GlobalKeyword != default)
            {
                continue;
            }

            var isInsideNamespaceNode = usingDirective.Parent is NamespaceDeclarationSyntax;

            if (IsInsideNamespace(usingDirective, config))
            {
                if (!isInsideNamespaceNode)
                {
                    var nameText = usingDirective.Name?.ToString() ?? string.Empty;
                    var diagnostic = Diagnostic.Create(
                        UsingShouldBeInsideNamespaceRule,
                        usingDirective.GetLocation(),
                        nameText);

                    context.ReportDiagnostic(diagnostic);
                }
            }
            else
            {
                if (isInsideNamespaceNode)
                {
                    var nameText = usingDirective.Name?.ToString() ?? string.Empty;
                    var diagnostic = Diagnostic.Create(
                        UsingShouldBeFileScopedRule,
                        usingDirective.GetLocation(),
                        nameText);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static void AnalyzeFileScopedNamespace(
        SyntaxNodeAnalysisContext context,
        CompilationUnitSyntax compilationUnit,
        NamespaceUsingScopeConfig config)
    {
        var fileScopedNamespace = compilationUnit.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNamespace is null)
        {
            return;
        }

        // For file-scoped namespaces, usings after the namespace declaration are siblings
        // in the syntax tree (children of CompilationUnit), not children of the namespace.
        // We use position-based detection: a using is "inside" if it appears after the
        // namespace declaration's semicolon.
        var namespaceEnd = fileScopedNamespace.SemicolonToken.Span.End;

        foreach (var usingDirective in compilationUnit.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (usingDirective.GlobalKeyword != default)
            {
                continue;
            }

            // Position-based: using is "inside" if it starts after the namespace semicolon
            var isPositionedAfterNamespace = usingDirective.SpanStart > namespaceEnd;

            if (IsInsideNamespace(usingDirective, config))
            {
                // MungBean./Plate. usings should be after the namespace declaration
                if (!isPositionedAfterNamespace)
                {
                    var nameText = usingDirective.Name?.ToString() ?? string.Empty;
                    var diagnostic = Diagnostic.Create(
                        UsingShouldBeInsideNamespaceRule,
                        usingDirective.GetLocation(),
                        nameText);

                    context.ReportDiagnostic(diagnostic);
                }
            }
            else
            {
                // Non-MungBean/Plate usings should be before the namespace declaration
                if (isPositionedAfterNamespace)
                {
                    var nameText = usingDirective.Name?.ToString() ?? string.Empty;
                    var diagnostic = Diagnostic.Create(
                        UsingShouldBeFileScopedRule,
                        usingDirective.GetLocation(),
                        nameText);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool IsInsideNamespace(UsingDirectiveSyntax usingDirective, NamespaceUsingScopeConfig config)
    {
        if (usingDirective.Name is null)
        {
            return false;
        }

        var nameText = usingDirective.Name.ToString();

        foreach (var prefix in config.InsideNamespacePrefixes)
        {
            if (nameText.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class NamespaceUsingScopeConfig
{
    public ImmutableArray<string> InsideNamespacePrefixes { get; }

    private NamespaceUsingScopeConfig(ImmutableArray<string> insideNamespacePrefixes)
    {
        InsideNamespacePrefixes = insideNamespacePrefixes;
    }

    public static NamespaceUsingScopeConfig Load(AnalyzerOptions options, SyntaxTree syntaxTree)
    {
        if (options is null)
        {
            return new NamespaceUsingScopeConfig(ImmutableArray<string>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<string>();

        var provider = options.AnalyzerConfigOptionsProvider;
        if (provider is not null)
        {
            var treeOptions = provider.GetOptions(syntaxTree);

            if (treeOptions.TryGetValue("namespace_using_scope_inside_prefixes", out var rawValue))
            {
                foreach (var prefix in ParseEditorConfigPrefixes(rawValue))
                {
                    builder.Add(prefix);
                }
            }
        }

        if (builder.Count > 0)
        {
            return new NamespaceUsingScopeConfig(builder.ToImmutable());
        }

        // Fallback to JSON-based configuration from AdditionalFiles for scenarios
        // where .editorconfig is not used (e.g., tests or older consumers).
        return Load(options.AdditionalFiles);
    }

    public static NamespaceUsingScopeConfig Load(ImmutableArray<AdditionalText> additionalFiles)
    {
        // Look for a config file named "NamespaceUsingScope.config.json" in AdditionalFiles.
        foreach (var file in additionalFiles)
        {
            var path = file.Path;

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var fileName = System.IO.Path.GetFileName(path);

            if (!string.Equals(fileName, "NamespaceUsingScope.config.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = file.GetText();
            if (text is null)
            {
                continue;
            }

            var prefixes = ParseInsideNamespacePrefixes(text.ToString()).ToImmutableArray();

            // Only return if JSON actually provided prefixes; otherwise fall through to defaults
            if (!prefixes.IsDefaultOrEmpty)
            {
                return new NamespaceUsingScopeConfig(prefixes);
            }
        }

        // Final safety-net fallback: if neither .editorconfig nor JSON provides prefixes,
        // default to MungBean. and Plate. prefixes. This ensures the analyzer functions
        // for mung-bean and related projects even when config isn't flowing correctly.
        // Other repos can override via .editorconfig or JSON, or disable NSUSG diagnostics.
        return new NamespaceUsingScopeConfig(DefaultInsideNamespacePrefixes);
    }

    /// <summary>
    /// Default prefixes used when neither .editorconfig nor JSON config provides values.
    /// Primarily for mung-bean and Plate ecosystem projects.
    /// </summary>
    private static readonly ImmutableArray<string> DefaultInsideNamespacePrefixes =
        ImmutableArray.Create("MungBean.", "Plate.");

    private static IEnumerable<string> ParseEditorConfigPrefixes(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        var separators = new[] { ';', ',' };
        var segments = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                yield return trimmed;
            }
        }
    }

    private static IEnumerable<string> ParseInsideNamespacePrefixes(string json)
    {
        // Simple, robust-enough parser for a config of the form:
        // { "insideNamespacePrefixes": [ "MungBean.", "MyGame." ] }
        // We avoid a full JSON dependency for analyzers.

        if (string.IsNullOrWhiteSpace(json))
        {
            yield break;
        }

        var key = "\"insideNamespacePrefixes\"";
        var index = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            yield break;
        }

        var arrayStart = json.IndexOf('[', index);
        if (arrayStart < 0)
        {
            yield break;
        }

        var arrayEnd = json.IndexOf(']', arrayStart + 1);
        if (arrayEnd < 0 || arrayEnd <= arrayStart + 1)
        {
            yield break;
        }

        var arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
        var segments = arrayContent.Split(',');

        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                yield return trimmed;
            }
        }
    }
}
