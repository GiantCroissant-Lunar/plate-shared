using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Plate.SCG.Resilience.Decorator;

internal static class Utility
{
    public static bool Predicate(
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (node is not InterfaceDeclarationSyntax iface)
        {
            return false;
        }

        if (iface.AttributeLists.Count == 0)
        {
            return false;
        }

        foreach (var list in iface.AttributeLists)
        {
            foreach (var attr in list.Attributes)
            {
                var nameText = GetSimpleName(attr.Name);
                if (string.Equals(nameText, "Resilient", StringComparison.Ordinal) ||
                    string.Equals(nameText, "ResilientAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static INamedTypeSymbol? Transform(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Node is not InterfaceDeclarationSyntax iface)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(iface, cancellationToken) as INamedTypeSymbol;
    }

    public static string GetFullNamespace(INamespaceSymbol ns)
    {
        if (ns is null || ns.IsGlobalNamespace)
        {
            return string.Empty;
        }

        var parts = ns.ToDisplayString().Split('.');
        return string.Join(".", parts);
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
