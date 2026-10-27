// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Finds the <c language="csharp">global::</c>-qualified types a generated file names outside its authored text. A
/// qualified name is read the way C# reads it: each leading segment that names a known namespace is a namespace, and
/// the next segment is the type. A name whose namespace or type is not known is refused.
/// </summary>
/// <param name="root">The parsed file.</param>
/// <param name="generated">The generated type inventory.</param>
/// <param name="project">The project the file compiles into.</param>
/// <param name="authored">The spans of authored text.</param>
internal sealed class QualifiedReferences(CompilationUnitSyntax root, IReadOnlyCollection<GeneratedTypeName> generated, GeneratedProjects project, IReadOnlyList<TextSpan> authored)
{
    readonly FrameworkNameCatalog _catalog = FrameworkNameCatalog.Current;
    readonly HashSet<string> _namespaces = [.. generated.SelectMany(type => CatalogNames.SelfAndContaining(type.Namespace)).Where(@namespace => @namespace.Length > 0)];
    readonly HashSet<(string Namespace, string Name, int Arity)> _types = [.. generated.Select(type => (type.Namespace, type.Name, type.Arity))];

    /// <summary>
    /// Finds the references.
    /// </summary>
    /// <returns>Each reference with the span its spelling replaces: from <c language="csharp">global::</c> to the end of the type's identifier.</returns>
    public IReadOnlyList<QualifiedReference> Find()
    {
        foreach (var @namespace in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        {
            _namespaces.UnionWith(CatalogNames.SelfAndContaining(@namespace.Name.ToString()).Where(name => name.Length > 0));
        }

        return [.. root.DescendantNodes().OfType<AliasQualifiedNameSyntax>()
            .Where(name => name.Alias.Identifier.ValueText == "global" && !authored.Any(span => span.IntersectsWith(name.Span)) &&
                !name.Ancestors().Any(ancestor => ancestor is UsingDirectiveSyntax || (ancestor is BaseNamespaceDeclarationSyntax declaration && declaration.Name.Span.Contains(name.Span))))
            .Select(Reference)
            .OfType<QualifiedReference>()];
    }

    static (SyntaxNode Node, SimpleNameSyntax Segment)? Next(SyntaxNode node) => node.Parent switch
    {
        QualifiedNameSyntax qualified when qualified.Left == node => (qualified, qualified.Right),
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } access when access.Expression == node => (access, access.Name),
        _ => null
    };

    static ReferenceContext Context(SyntaxNode node) => node.Parent switch
    {
        AttributeSyntax attribute when attribute.Name == node => ReferenceContext.Attribute,
        _ when IsNameOfArgument(node) => ReferenceContext.NameOf,
        _ when node is ExpressionSyntax expression && SyntaxFacts.IsInTypeOnlyContext(expression) => ReferenceContext.Type,
        _ => ReferenceContext.Expression
    };

    static bool IsNameOfArgument(SyntaxNode node) => node.Ancestors().OfType<InvocationExpressionSyntax>()
        .Any(invocation => invocation.Expression is IdentifierNameSyntax name &&
            string.Equals(name.Identifier.ValueText, "nameof", StringComparison.Ordinal) && invocation.ArgumentList.Span.Contains(node.Span));

    static string MetadataName(SimpleNameSyntax name) => name is GenericNameSyntax generic ?
        $"{name.Identifier.ValueText}`{generic.TypeArgumentList.Arguments.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : name.Identifier.ValueText;

    QualifiedReference? Reference(AliasQualifiedNameSyntax alias)
    {
        SyntaxNode node = alias;
        var segment = alias.Name;
        var @namespace = string.Empty;
        while (true)
        {
            var next = Next(node);
            var candidate = CatalogNames.Qualify(@namespace, segment.Identifier.ValueText);
            if (segment is IdentifierNameSyntax && next is not null && IsNamespace(candidate))
            {
                @namespace = candidate;
                (node, segment) = next.Value;
                continue;
            }

            var arity = segment is GenericNameSyntax generic ? generic.TypeArgumentList.Arguments.Count : 0;
            var context = Context(node);
            var typeName = segment.Identifier.ValueText;

            // [global::N.Name] names N.NameAttribute when N.Name is not an attribute type itself.
            if (!IsType(@namespace, typeName, arity) && context == ReferenceContext.Attribute) typeName += "Attribute";
            if (!IsType(@namespace, typeName, arity)) throw new CSharpImportRejected($"The generated type '{CatalogNames.Qualify(@namespace, MetadataName(segment))}' is not cataloged.");
            VerifyNested(node, @namespace, MetadataName(segment));
            var type = new TypeReference(@namespace, typeName, arity, context);
            return new(type, TextSpan.FromBounds(alias.SpanStart, segment.Identifier.Span.End));
        }
    }

    void VerifyNested(SyntaxNode node, string @namespace, string metadataName)
    {
        while (Next(node) is { Node: QualifiedNameSyntax qualified, Segment: var nested })
        {
            metadataName += "+" + MetadataName(nested);
            if (!_catalog.Content.Types.Any(type => type.Namespace == @namespace && type.MetadataName == metadataName && (type.Projects & project) != GeneratedProjects.None))
                throw new CSharpImportRejected($"The generated nested type '{@namespace}.{metadataName}' is not cataloged.");
            node = qualified;
        }
    }

    bool IsNamespace(string @namespace) => _namespaces.Contains(@namespace) || _catalog.HasNamespace(@namespace, project);

    bool IsType(string @namespace, string name, int arity) =>
        _types.Contains((@namespace, name, arity)) || _catalog.TypesNamed(@namespace, name, arity, project).Any();
}
