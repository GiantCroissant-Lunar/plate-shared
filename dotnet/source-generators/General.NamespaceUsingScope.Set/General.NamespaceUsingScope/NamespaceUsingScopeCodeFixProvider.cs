using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace PlateShared.SCG.General.NamespaceUsingScope;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamespaceUsingScopeCodeFixProvider))]
[Shared]
public sealed class NamespaceUsingScopeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId,
            NamespaceUsingScopeAnalyzer.UsingShouldBeFileScopedId,
            NamespaceUsingScopeAnalyzer.UsingOrderInvalidId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var span = diagnostic.Location.SourceSpan;
        var node = root.FindNode(span, getInnermostNodeForTie: true);

        if (node is not UsingDirectiveSyntax usingDirective)
        {
            return;
        }

        var compilationUnit = root as CompilationUnitSyntax;

        switch (diagnostic.Id)
        {
            case NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId:
                if (compilationUnit is null)
                {
                    return;
                }

                // Support both traditional block-scoped namespaces and file-scoped namespaces.
                if (compilationUnit.Members.OfType<FileScopedNamespaceDeclarationSyntax>().Any())
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Move using inside namespace",
                            cancellationToken => MoveUsingInsideFileScopedNamespaceAsync(context.Document, compilationUnit, usingDirective, cancellationToken),
                            nameof(NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId) + "_FileScoped"),
                        diagnostic);
                }
                else if (compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().Any())
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Move using inside namespace",
                            cancellationToken => MoveUsingInsideNamespaceAsync(context.Document, root, usingDirective, cancellationToken),
                            nameof(NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId)),
                        diagnostic);
                }

                break;

            case NamespaceUsingScopeAnalyzer.UsingShouldBeFileScopedId:
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Move using to file scope",
                        cancellationToken => MoveUsingToFileScopeAsync(context.Document, root, usingDirective, cancellationToken),
                        nameof(NamespaceUsingScopeAnalyzer.UsingShouldBeFileScopedId)),
                    diagnostic);
                break;

            case NamespaceUsingScopeAnalyzer.UsingOrderInvalidId:
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Reorder using directives by namespace scope",
                        cancellationToken => ReorderFileScopedUsingsAsync(context.Document, root, usingDirective, cancellationToken),
                        nameof(NamespaceUsingScopeAnalyzer.UsingOrderInvalidId)),
                    diagnostic);
                break;
        }
    }

    private static Task<Document> MoveUsingInsideNamespaceAsync(
        Document document,
        SyntaxNode root,
        UsingDirectiveSyntax usingDirective,
        CancellationToken cancellationToken)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return Task.FromResult(document);
        }

        var namespaceDeclaration = compilationUnit.Members
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (namespaceDeclaration is null)
        {
            // Nothing to do if there is no block-scoped namespace.
            return Task.FromResult(document);
        }

        // Remove from file-scoped usings
        var newCompilationUnit = compilationUnit.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia);

        // Add to namespace usings, preserving trivia
        namespaceDeclaration = newCompilationUnit.Members
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault() ?? namespaceDeclaration;

        // Compute indentation based on existing namespace children (usings or members),
        // falling back to 4 spaces if nothing is available.
        var indentTrivia = GetNamespaceChildIndentTrivia(namespaceDeclaration);
        var indentedUsing = usingDirective.WithLeadingTrivia(indentTrivia);

        var updatedNamespace = namespaceDeclaration.WithUsings(namespaceDeclaration.Usings.Add(indentedUsing));
        newCompilationUnit = newCompilationUnit.ReplaceNode(namespaceDeclaration, updatedNamespace);

        var newRoot = (SyntaxNode)newCompilationUnit;
        var updatedDocument = document.WithSyntaxRoot(newRoot);
        return Formatter.FormatAsync(updatedDocument, cancellationToken: cancellationToken);
    }

    private static Task<Document> MoveUsingInsideFileScopedNamespaceAsync(
        Document document,
        CompilationUnitSyntax compilationUnit,
        UsingDirectiveSyntax usingDirective,
        CancellationToken cancellationToken)
    {
        var fileScopedNamespace = compilationUnit.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNamespace is null)
        {
            return Task.FromResult(document);
        }

        // Remove the using completely (no trivia kept) to avoid orphaned blank lines
        var newCompilationUnit = compilationUnit.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia);
        if (newCompilationUnit is null)
        {
            return Task.FromResult(document);
        }

        fileScopedNamespace = newCompilationUnit.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault() ?? fileScopedNamespace;

        // Strip original trivia and add a leading newline for blank line after namespace
        var formattedUsing = usingDirective
            .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed))
            .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed));

        // Normalize namespace leading trivia (remove extra blank lines)
        var normalizedNamespace = fileScopedNamespace.WithLeadingTrivia(SyntaxFactory.TriviaList());
        var updatedNamespace = normalizedNamespace.WithUsings(normalizedNamespace.Usings.Add(formattedUsing));
        newCompilationUnit = newCompilationUnit.ReplaceNode(fileScopedNamespace, updatedNamespace);

        var newRoot = (SyntaxNode)newCompilationUnit;
        var updatedDocument = document.WithSyntaxRoot(newRoot);
        return Formatter.FormatAsync(updatedDocument, cancellationToken: cancellationToken);
    }

    private static Task<Document> MoveUsingToFileScopeAsync(
        Document document,
        SyntaxNode root,
        UsingDirectiveSyntax usingDirective,
        CancellationToken cancellationToken)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return Task.FromResult(document);
        }

        var namespaceDeclaration = usingDirective.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
        if (namespaceDeclaration is not null)
        {
            // Block-scoped namespace case.
            var updatedNamespace = namespaceDeclaration.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia);
            var intermediateRoot = compilationUnit.ReplaceNode(namespaceDeclaration, updatedNamespace);

            // Insert at file scope after any existing file-scope usings
            var usings = intermediateRoot.Usings;
            var insertIndex = usings.Count;
            var updatedCompilationUnit = intermediateRoot.WithUsings(usings.Insert(insertIndex, usingDirective));

            var newRoot = (SyntaxNode)updatedCompilationUnit;
            var updatedDocument = document.WithSyntaxRoot(newRoot);
            return Formatter.FormatAsync(updatedDocument, cancellationToken: cancellationToken);
        }

        // File-scoped namespace case: usings after "namespace X;" are siblings of the namespace
        // in the syntax tree (children of CompilationUnitSyntax), not children of the namespace.
        // We need to:
        // 1. Remove the using from its current position
        // 2. Insert it at the top of the file (before the namespace declaration)
        var fileScopedNamespace = compilationUnit.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNamespace is null)
        {
            return Task.FromResult(document);
        }

        // Check if this using is positioned after the namespace (sibling case)
        var namespaceEnd = fileScopedNamespace.SemicolonToken.Span.End;
        if (usingDirective.SpanStart > namespaceEnd)
        {
            // The using is a sibling of the namespace in CompilationUnit.Members
            // We need to remove it from Members and add it to CompilationUnit.Usings
            var usingAsMember = compilationUnit.Members
                .OfType<UsingDirectiveSyntax>()
                .FirstOrDefault(u => u.SpanStart == usingDirective.SpanStart);

            if (usingAsMember is null)
            {
                // The using might be in a different location; try removing directly
                var newCompilationUnit = compilationUnit.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia);
                if (newCompilationUnit is null)
                {
                    return Task.FromResult(document);
                }

                // Add to file-scope usings at the end (before namespace)
                var cleanUsing = usingDirective.WithLeadingTrivia(SyntaxFactory.TriviaList())
                    .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed));
                var updatedUsings = newCompilationUnit.Usings.Add(cleanUsing);
                var finalCompilationUnit = newCompilationUnit.WithUsings(updatedUsings);

                var finalDocument = document.WithSyntaxRoot(finalCompilationUnit);
                return Formatter.FormatAsync(finalDocument, cancellationToken: cancellationToken);
            }
        }

        // Fallback: try to handle as child of file-scoped namespace (legacy path)
        var fileScopedNs = usingDirective.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>();
        if (fileScopedNs is not null)
        {
            var updatedFileScopedNamespace = fileScopedNs.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia);
            var intermediateCompilationUnit = compilationUnit.ReplaceNode(fileScopedNs, updatedFileScopedNamespace);

            var fileScopeUsings = intermediateCompilationUnit.Usings;
            var fileScopeInsertIndex = fileScopeUsings.Count;
            var finalCompilationUnit = intermediateCompilationUnit.WithUsings(fileScopeUsings.Insert(fileScopeInsertIndex, usingDirective));

            var finalRoot = (SyntaxNode)finalCompilationUnit;
            var finalDocument = document.WithSyntaxRoot(finalRoot);
            return Formatter.FormatAsync(finalDocument, cancellationToken: cancellationToken);
        }

        return Task.FromResult(document);
    }

    private static Task<Document> ReorderFileScopedUsingsAsync(
        Document document,
        SyntaxNode root,
        UsingDirectiveSyntax _,
        CancellationToken cancellationToken)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return Task.FromResult(document);
        }

        // Recompute the desired ordering based on the same logic as the analyzer.
        var additionalFiles = document.Project.AnalyzerOptions.AdditionalFiles;
        var config = NamespaceUsingScopeConfig.Load(additionalFiles);

        if (config.InsideNamespacePrefixes.IsDefaultOrEmpty)
        {
            return Task.FromResult(document);
        }

        var globalUsings = new List<UsingDirectiveSyntax>();
        var nonGlobalUsings = new List<UsingDirectiveSyntax>();

        foreach (var usingDirective in compilationUnit.Usings)
        {
            if (usingDirective.GlobalKeyword != default)
            {
                globalUsings.Add(usingDirective);
            }
            else
            {
                nonGlobalUsings.Add(usingDirective);
            }
        }

        var outsideUsings = new List<UsingDirectiveSyntax>();
        var insideUsings = new List<UsingDirectiveSyntax>();

        foreach (var usingDirective in nonGlobalUsings)
        {
            if (IsInsideNamespace(usingDirective, config))
            {
                insideUsings.Add(usingDirective);
            }
            else
            {
                outsideUsings.Add(usingDirective);
            }
        }

        if (insideUsings.Count == 0 || outsideUsings.Count == 0)
        {
            // Nothing to reorder if all usings are in one group.
            return Task.FromResult(document);
        }

        var newUsings = new List<UsingDirectiveSyntax>();
        newUsings.AddRange(globalUsings);
        newUsings.AddRange(outsideUsings);
        newUsings.AddRange(insideUsings);

        var newCompilationUnit = compilationUnit.WithUsings(SyntaxFactory.List(newUsings));
        var newRoot = (SyntaxNode)newCompilationUnit;
        var updatedDocument = document.WithSyntaxRoot(newRoot);
        return Formatter.FormatAsync(updatedDocument, cancellationToken: cancellationToken);
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
            if (nameText.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static SyntaxTriviaList GetNamespaceChildIndentTrivia(NamespaceDeclarationSyntax namespaceDeclaration)
    {
        // Prefer indentation of the first existing using inside the namespace, if any.
        var existingUsing = namespaceDeclaration.Usings.FirstOrDefault();
        if (existingUsing is not null)
        {
            var indent = ExtractIndentTrivia(existingUsing.GetLeadingTrivia());
            if (indent.Count > 0)
            {
                return indent;
            }
        }

        // Otherwise, fall back to indentation of the first member.
        var firstMember = namespaceDeclaration.Members.FirstOrDefault();
        if (firstMember is not null)
        {
            var indent = ExtractIndentTrivia(firstMember.GetLeadingTrivia());
            if (indent.Count > 0)
            {
                return indent;
            }
        }

        // Final fallback: 4 spaces.
        return SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("    "));
    }

    private static SyntaxTriviaList ExtractIndentTrivia(SyntaxTriviaList triviaList)
    {
        // Look for the last whitespace trivia in the sequence, which typically encodes indentation.
        var whitespace = triviaList.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
        return whitespace.RawKind == 0
            ? SyntaxFactory.TriviaList()
            : SyntaxFactory.TriviaList(whitespace);
    }
}
