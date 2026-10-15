// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// Reads, from one rendered file, the names a renderer knows are in scope there: the members, parameters, locals, type
/// parameters and nested types it declares, plus what its types inherit — from the framework name catalog for a
/// framework base class, and from the declaration for a generated one. It also lists the simple names and member
/// accesses the file writes outside the references the resolver spells.
/// </summary>
/// <param name="ValueNames">Members, parameters and locals in scope.</param>
/// <param name="TypeNames">Type parameters and nested types in scope.</param>
/// <param name="PinnedNames">Simple names written outside the spelled references.</param>
/// <param name="MemberAccessNames">Member names accessed with a dot.</param>
/// <param name="IsUnknown">Whether a base class is neither generated nor cataloged.</param>
sealed record QualifiedFileScope(
    IReadOnlySet<string> ValueNames,
    IReadOnlySet<string> TypeNames,
    IReadOnlySet<string> PinnedNames,
    IReadOnlySet<string> MemberAccessNames,
    bool IsUnknown)
{
    // Records synthesize these members; every class inherits object's, which the catalog walk adds.
    static readonly string[] _recordMembers = ["EqualityContract", "PrintMembers", "Deconstruct", "Equals", "GetHashCode", "ToString"];

    /// <summary>
    /// Reads the scope of a file.
    /// </summary>
    /// <param name="model">The semantic model of the file, used only to resolve base types.</param>
    /// <param name="project">The project the file compiles into.</param>
    /// <param name="spelled">The spans of the qualified references the resolver spells.</param>
    /// <returns>The scope.</returns>
    public static QualifiedFileScope Read(SemanticModel model, GeneratedProjects project, IReadOnlyList<Microsoft.CodeAnalysis.Text.TextSpan> spelled)
    {
        var root = model.SyntaxTree.GetRoot();
        var values = new HashSet<string>(StringComparer.Ordinal);
        var types = new HashSet<string>(StringComparer.Ordinal);
        var unknown = false;
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case TypeDeclarationSyntax type:
                    if (type.Parent is TypeDeclarationSyntax) types.Add(type.Identifier.ValueText);
                    types.UnionWith(type.TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText) ?? []);
                    if (type is RecordDeclarationSyntax) values.UnionWith(_recordMembers);
                    unknown |= !Inherit(model.GetDeclaredSymbol(type), project, values, types);
                    break;
                case EnumDeclarationSyntax nested when nested.Parent is TypeDeclarationSyntax: types.Add(nested.Identifier.ValueText); break;
                case DelegateDeclarationSyntax nested when nested.Parent is TypeDeclarationSyntax: types.Add(nested.Identifier.ValueText); break;
                case MethodDeclarationSyntax method:
                    values.Add(method.Identifier.ValueText);
                    types.UnionWith(method.TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText) ?? []);
                    break;
                case LocalFunctionStatementSyntax function:
                    values.Add(function.Identifier.ValueText);
                    types.UnionWith(function.TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText) ?? []);
                    break;
                case PropertyDeclarationSyntax property: values.Add(property.Identifier.ValueText); break;
                case EventDeclarationSyntax @event: values.Add(@event.Identifier.ValueText); break;
                case VariableDeclaratorSyntax variable: values.Add(variable.Identifier.ValueText); break;
                case ParameterSyntax parameter: values.Add(parameter.Identifier.ValueText); break;
                case SingleVariableDesignationSyntax designation: values.Add(designation.Identifier.ValueText); break;
                case ForEachStatementSyntax loop: values.Add(loop.Identifier.ValueText); break;
                case CatchDeclarationSyntax @catch: values.Add(@catch.Identifier.ValueText); break;
                case EnumMemberDeclarationSyntax member: values.Add(member.Identifier.ValueText); break;
            }
        }

        values.Remove(string.Empty);
        return new(values, types, Pinned(root, spelled), MemberAccesses(root), unknown);
    }

    static bool Inherit(INamedTypeSymbol? type, GeneratedProjects project, HashSet<string> values, HashSet<string> types)
    {
        for (var current = type?.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Locations.Any(location => location.IsInMetadata))
            {
                var inherited = FrameworkNameCatalog.Current.Inherited(MetadataName(current.OriginalDefinition), project);
                if (inherited is null) return false;
                values.UnionWith(inherited.Members);
                types.UnionWith(inherited.NestedTypes);
                return true;
            }

            // A generated base type: the renderer knows what it declares.
            foreach (var member in current.GetMembers().Where(member => !member.IsImplicitlyDeclared || member is IPropertySymbol))
            {
                if (member is INamedTypeSymbol) types.Add(member.Name);
                else values.Add(member.Name);
            }
        }

        return true;
    }

    static string MetadataName(INamedTypeSymbol type)
    {
        var names = new List<string>();
        for (var current = type; current is not null; current = current.ContainingType) names.Insert(0, current.MetadataName);
        var @namespace = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
        return CatalogNames.Qualify(@namespace, string.Join('+', names));
    }

    static HashSet<string> Pinned(SyntaxNode root, IReadOnlyList<Microsoft.CodeAnalysis.Text.TextSpan> spelled) =>
    [
        .. root.DescendantNodes().OfType<SimpleNameSyntax>()
            .Where(name => !spelled.Any(span => span.Contains(name.Identifier.Span)) && name.Parent switch
            {
                MemberAccessExpressionSyntax access => access.Name != name,
                MemberBindingExpressionSyntax => false,
                QualifiedNameSyntax qualified => qualified.Right != name,
                AliasQualifiedNameSyntax => false,
                NameColonSyntax or NameEqualsSyntax => false,
                _ => true
            })
            .Select(name => name.Identifier.ValueText)
    ];

    static HashSet<string> MemberAccesses(SyntaxNode root) =>
    [
        .. root.DescendantNodes().Select(node => node switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
            _ => null
        }).OfType<string>()
    ];
}
#endif
