// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// Rewrites one file of today's fully qualified output the way a converted renderer would emit it: every
/// <c language="csharp">global::</c>-qualified type is offered to the <see cref="ImportResolver"/> with what a renderer
/// knows about the file, the resolver's using directives replace the file's own, and each reference takes the
/// resolver's spelling. Nothing else in the file changes, so authored bodies stay verbatim.
/// </summary>
static class QualifiedFileRewrite
{
    /// <summary>
    /// Rewrites a file.
    /// </summary>
    /// <param name="model">The semantic model of the original file.</param>
    /// <param name="project">The project the file compiles into.</param>
    /// <param name="generated">The generated type inventory of the project.</param>
    /// <returns>The text changes, ordered by position, and the number of references spelled short.</returns>
    public static (ImmutableArray<TextChange> Changes, int ShortSpellings) Rewrite(SemanticModel model, GeneratedProjects project, IEnumerable<GeneratedTypeName> generated)
    {
        var root = model.SyntaxTree.GetCompilationUnitRoot();
        var references = root.DescendantNodes().OfType<AliasQualifiedNameSyntax>()
            .Where(name => name.Alias.Identifier.ValueText == "global")
            .Select(name => Reference(model, name))
            .OfType<QualifiedReference>()
            .ToArray();
        var namespaces = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().ToArray();
        var scope = QualifiedFileScope.Read(model, project, [.. references.SelectMany(reference => reference.Spans)]);
        var request = new ImportRequest(namespaces.Length == 1 ? namespaces[0].Name.ToString() : string.Empty, project, generated, references.Select(reference => reference.Type))
        {
            RequiredImports = root.Usings.Where(@using => @using.Alias is null && @using.StaticKeyword.IsKind(SyntaxKind.None)).Select(@using => @using.NamespaceOrType.ToString()),
            ScopeTypeNames = scope.TypeNames,
            ScopeValueNames = scope.ValueNames,
            PinnedNames = scope.PinnedNames,
            MemberAccessNames = scope.MemberAccessNames,
            HasUnknownScope = scope.IsUnknown || namespaces.Length > 1 || root.Usings.Any(@using => @using.Alias is not null || !@using.StaticKeyword.IsKind(SyntaxKind.None))
        };
        var resolution = ImportResolver.Resolve(request);
        var changes = new List<TextChange>();
        var spelledShort = 0;
        foreach (var reference in references)
        {
            var spelling = resolution.Spell(reference.Type);
            if (spelling.StartsWith("global::", StringComparison.Ordinal)) continue;
            spelledShort++;
            changes.Add(new(TextSpan.FromBounds(reference.Node.SpanStart, reference.Name.SpanStart), string.Empty));
            if (spelling != reference.Name.Identifier.Text) changes.Add(new(reference.Name.Identifier.Span, spelling));
        }

        if (resolution.Usings.Length > 0 && namespaces is [FileScopedNamespaceDeclarationSyntax fileNamespace])
        {
            var lines = string.Concat(resolution.Usings.Select(@namespace => $"\nusing {@namespace};"));
            changes.Add(new(new(fileNamespace.SemicolonToken.Span.End, 0), $"\n{lines}\n"));
        }
        return ([.. changes.Where(change => change.Span.Length > 0 || change.NewText!.Length > 0).OrderBy(change => change.Span.Start)], spelledShort);
    }

    static QualifiedReference? Reference(SemanticModel model, AliasQualifiedNameSyntax name)
    {
        for (SyntaxNode node = name; ; )
        {
            var symbol = Symbol(model, node);
            if (symbol is INamedTypeSymbol type) return Create(node, type.OriginalDefinition);
            if (symbol is not INamespaceSymbol) return null;
            node = node.Parent switch
            {
                QualifiedNameSyntax qualified when qualified.Left == node => qualified,
                MemberAccessExpressionSyntax access when access.Expression == node => access,
                _ => null!
            };
            if (node is null) return null;
        }
    }

    static ISymbol? Symbol(SemanticModel model, SyntaxNode node)
    {
        // A candidate is not a binding: a namespace/type collision can occur in the middle of a qualified name.
        // Rewriting an arbitrarily chosen type candidate would reinterpret the remaining namespace segments as members.
        var symbol = model.GetSymbolInfo(node).Symbol;
        return symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor ? constructor.ContainingType : symbol;
    }

    static QualifiedReference Create(SyntaxNode node, INamedTypeSymbol type)
    {
        var name = node switch
        {
            QualifiedNameSyntax qualified => qualified.Right,
            MemberAccessExpressionSyntax access => access.Name,
            AliasQualifiedNameSyntax alias => alias.Name,
            _ => throw new UnexpectedQualifiedName(node.ToString())
        };
        var context = node.Parent switch
        {
            AttributeSyntax attribute when attribute.Name == node => ReferenceContext.Attribute,
            _ when IsNameOfArgument(node) => ReferenceContext.NameOf,
            _ when node is ExpressionSyntax expression && SyntaxFacts.IsInTypeOnlyContext(expression) => ReferenceContext.Type,
            _ => ReferenceContext.Expression
        };
        var @namespace = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
        return new(new(@namespace, type.Name, type.Arity, context), node, name);
    }

    static bool IsNameOfArgument(SyntaxNode node) => node.Ancestors().OfType<InvocationExpressionSyntax>()
        .Any(invocation => invocation.Expression is IdentifierNameSyntax name &&
            string.Equals(name.Identifier.ValueText, "nameof", StringComparison.Ordinal) && invocation.ArgumentList.Span.Contains(node.Span));
}
#endif
