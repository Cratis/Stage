// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Reads, from one generated file, the names in scope there: the members, parameters, locals, range variables, type
/// parameters and nested types it declares, plus what its types inherit from a cataloged base class. It also lists the
/// simple names and member accesses the file writes outside the references the resolver spells. A type deriving from a
/// class whose members are not known makes the scope unknown.
/// </summary>
/// <param name="ValueNames">Members, parameters and locals in scope.</param>
/// <param name="TypeNames">Type parameters and nested types in scope.</param>
/// <param name="PinnedNames">Simple names written outside the spelled references.</param>
/// <param name="MemberAccessNames">Member names accessed with a dot.</param>
/// <param name="IsUnknown">Whether a base class is neither declared in the file nor cataloged.</param>
internal sealed record FileScope(
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
    /// <param name="root">The parsed file.</param>
    /// <param name="fileNamespace">The file's namespace.</param>
    /// <param name="generated">The generated type inventory, including the file's own declarations.</param>
    /// <param name="project">The project the file compiles into.</param>
    /// <param name="spelled">The spans of the qualified references the resolver spells.</param>
    /// <returns>The scope.</returns>
    public static FileScope Read(CompilationUnitSyntax root, string fileNamespace, IReadOnlyCollection<GeneratedTypeName> generated, GeneratedProjects project, IReadOnlyList<TextSpan> spelled)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        var types = new HashSet<string>(StringComparer.Ordinal);
        var declared = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Select(type => type.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        var unknown = false;
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case TypeDeclarationSyntax type:
                    if (type.Parent is TypeDeclarationSyntax) types.Add(type.Identifier.ValueText);
                    types.UnionWith(type.TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText) ?? []);
                    if (type is RecordDeclarationSyntax) values.UnionWith(_recordMembers);
                    unknown |= !Inherit(type, generated, declared, project, values, types);
                    break;
                case EnumDeclarationSyntax nested when nested.Parent is TypeDeclarationSyntax: types.Add(nested.Identifier.ValueText); break;
                case DelegateDeclarationSyntax @delegate:
                    if (@delegate.Parent is TypeDeclarationSyntax) types.Add(@delegate.Identifier.ValueText);
                    types.UnionWith(@delegate.TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText) ?? []);
                    break;
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
                case FromClauseSyntax from: values.Add(from.Identifier.ValueText); break;
                case LetClauseSyntax let: values.Add(let.Identifier.ValueText); break;
                case JoinClauseSyntax join: values.Add(join.Identifier.ValueText); break;
                case JoinIntoClauseSyntax into: values.Add(into.Identifier.ValueText); break;
                case QueryContinuationSyntax continuation: values.Add(continuation.Identifier.ValueText); break;
            }
        }

        values.Remove(string.Empty);
        return new(values, types, Pinned(root, spelled), MemberAccesses(root), unknown);
    }

    static bool Inherit(TypeDeclarationSyntax type, IReadOnlyCollection<GeneratedTypeName> generated, HashSet<string> declared, GeneratedProjects project, HashSet<string> values, HashSet<string> types)
    {
        if (type is InterfaceDeclarationSyntax) return type.BaseList is null;
        if (type is StructDeclarationSyntax || (type is RecordDeclarationSyntax record && record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)))
        {
            return Cataloged("System.ValueType", project, values, types);
        }

        if (type.BaseList?.Types.FirstOrDefault()?.Type is not { } baseType) return Cataloged("System.Object", project, values, types);
        if (baseType is SimpleNameSyntax simple)
        {
            // A base declared in the file contributes the members the file already lists.
            return declared.Contains(simple.Identifier.ValueText);
        }

        if (Qualified(baseType) is not var (@namespace, name, arity)) return false;
        if (generated.Contains(new(@namespace, name, arity))) return declared.Contains(name);
        var cataloged = FrameworkNameCatalog.Current.TypesNamed(@namespace, name, arity, project).ToArray();
        if (cataloged.Length != 1) return false;
        return cataloged[0].Kind == "interface"
            ? Cataloged("System.Object", project, values, types)
            : Cataloged(CatalogNames.Qualify(@namespace, CatalogNames.MetadataName(name, arity)), project, values, types);
    }

    // Reads global::A.B.Name<T> as the namespace, the name and the arity, the way the declaration wrote it.
    static (string Namespace, string Name, int Arity)? Qualified(TypeSyntax type)
    {
        var segments = new List<string>();
        SimpleNameSyntax last;
        switch (type)
        {
            case QualifiedNameSyntax qualified:
                last = qualified.Right;
                for (var current = qualified.Left; ;)
                {
                    if (current is QualifiedNameSyntax left)
                    {
                        segments.Insert(0, left.Right.Identifier.ValueText);
                        current = left.Left;
                    }
                    else if (current is AliasQualifiedNameSyntax alias && IsGlobal(alias) && alias.Name is IdentifierNameSyntax first)
                    {
                        segments.Insert(0, first.Identifier.ValueText);
                        break;
                    }
                    else
                    {
                        return null;
                    }
                }

                break;
            case AliasQualifiedNameSyntax global when IsGlobal(global):
                last = global.Name;
                break;
            default:
                return null;
        }

        return (string.Join('.', segments), last.Identifier.ValueText, last is GenericNameSyntax generic ? generic.TypeArgumentList.Arguments.Count : 0);
    }

    static bool IsGlobal(AliasQualifiedNameSyntax alias) => string.Equals(alias.Alias.Identifier.ValueText, "global", StringComparison.Ordinal);

    static bool Cataloged(string fullName, GeneratedProjects project, HashSet<string> values, HashSet<string> types)
    {
        var inherited = FrameworkNameCatalog.Current.Inherited(fullName, project);
        if (inherited is null) return false;
        values.UnionWith(inherited.Members);
        types.UnionWith(inherited.NestedTypes);
        return true;
    }

    static HashSet<string> Pinned(SyntaxNode root, IReadOnlyList<TextSpan> spelled) =>
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
