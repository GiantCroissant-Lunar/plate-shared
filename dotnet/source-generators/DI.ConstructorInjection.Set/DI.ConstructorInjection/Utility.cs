using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlateShared.SCG.DI.ConstructorInjection;

/// <summary>
/// Minimal utility helpers inlined here so this generator does not depend
/// on PlateShared.SCG.Shared.Abstractions. Only members actually used by
/// <see cref="SourceGenerator"/> are implemented.
/// </summary>
internal static class Utility
{
    public static bool PredicateAsync(
        SyntaxNode node,
        string attributeShortName,
        string attributeName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (node is not TypeDeclarationSyntax typeDecl)
        {
            return false;
        }

        if (typeDecl.AttributeLists.Count == 0)
        {
            return false;
        }

        foreach (var list in typeDecl.AttributeLists)
        {
            foreach (var attr in list.Attributes)
            {
                var nameText = GetSimpleName(attr.Name);

                if (string.Equals(nameText, attributeShortName, StringComparison.Ordinal) ||
                    string.Equals(nameText, attributeName, StringComparison.Ordinal) ||
                    string.Equals(nameText, attributeShortName + "Attribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static ITypeSymbol? TransformAsync(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Node is not TypeDeclarationSyntax typeDecl)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) as ITypeSymbol;
    }

    public static string CreateTypeDeclarationLine(ITypeSymbol typeSymbol)
    {
        var accessibility = typeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.Private => "private",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "internal",
        };

        var isRecord = typeSymbol.IsRecord;
        var keyword = (typeSymbol.TypeKind, isRecord) switch
        {
            (TypeKind.Class, true) => "record",
            (TypeKind.Class, false) => "class",
            (TypeKind.Struct, true) => "record struct",
            (TypeKind.Struct, false) => "struct",
            (TypeKind.Interface, _) => "interface",
            (TypeKind.Enum, _) => "enum",
            _ => "class",
        };

        const string modifiers = "partial";

        var name = typeSymbol.Name;
        if (typeSymbol is INamedTypeSymbol named && named.TypeArguments.Length > 0)
        {
            var typeArgs = string.Join(", ", named.TypeArguments
                .Select(t => t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            name = $"{name}<{typeArgs}>";
        }

        if ((typeSymbol as INamedTypeSymbol)?.IsReadOnly == true)
        {
            return $"{accessibility} readonly {modifiers} {keyword} {name}";
        }

        return $"{accessibility} {modifiers} {keyword} {name}";
    }

    public static string CreateCodeGenAttributes(
        string assemblyName,
        string assemblyVersion)
    {
        return $"    [global::System.CodeDom.Compiler.GeneratedCode(\"{assemblyName}\", \"{assemblyVersion}\")]\n" +
               "    [global::System.Diagnostics.DebuggerNonUserCode]\n";
    }

    public static string Indent(string text, int spaces)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var indent = new string(' ', spaces);
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        return string.Join("\n", lines.Select(l => indent + l));
    }

    public static List<ISymbol> GetAttributedSymbols(
        ITypeSymbol typeSymbol,
        string attributeShortName)
    {
        var results = new List<ISymbol>();

        foreach (var member in typeSymbol.GetMembers())
        {
            foreach (var attribute in member.GetAttributes())
            {
                var name = attribute.AttributeClass?.Name;
                if (name is null)
                {
                    continue;
                }

                if (string.Equals(name, attributeShortName, StringComparison.Ordinal) ||
                    string.Equals(name, attributeShortName + "Attribute", StringComparison.Ordinal))
                {
                    results.Add(member);
                    break;
                }
            }
        }

        return results;
    }

    private static string GetSimpleName(NameSyntax nameSyntax)
    {
        return nameSyntax switch
        {
            IdentifierNameSyntax ins => ins.Identifier.Text,
            QualifiedNameSyntax qns => qns.Right.Identifier.Text,
            AliasQualifiedNameSyntax aqns => aqns.Name.Identifier.Text,
            _ => nameSyntax.ToString(),
        };
    }
}
