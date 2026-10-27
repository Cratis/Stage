// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Turns a generated file written with fully qualified <c language="csharp">global::</c> type names into the file a
/// person would write: every qualified type the renderer wrote is offered to the <see cref="ImportResolver"/>, which
/// decides its using directives and spells each type by its short name wherever that provably binds to the same type.
/// The names in scope are read from the file itself. Authored text, such as a verbatim body, is never rewritten; its
/// names only constrain what the file may import.
/// </summary>
/// <param name="project">The project the file compiles into.</param>
/// <param name="generated">The complete renderer-owned declaration inventory.</param>
/// <param name="hasScaffoldGlobalUsings">Whether the file compiles with the scaffold-owned GlobalUsings.cs; see <see cref="ImportRequest.HasScaffoldGlobalUsings"/>.</param>
internal sealed class CSharpImports(GeneratedProjects project, IEnumerable<GeneratedTypeName> generated, bool hasScaffoldGlobalUsings)
{
    static readonly CSharpParseOptions _parseOptions = new(preprocessorSymbols: ["DEBUG"]);

    readonly HashSet<string> _types = new(StringComparer.Ordinal);
    readonly HashSet<string> _values = new(StringComparer.Ordinal);
    readonly HashSet<string> _pinned = new(StringComparer.Ordinal);
    readonly HashSet<string> _accessed = new(StringComparer.Ordinal);
    bool _unknown;

    internal void Scope(IEnumerable<string> types, IEnumerable<string> values)
    {
        _types.UnionWith(types);
        _values.UnionWith(values);
    }

    internal void Pin(IEnumerable<string> names) => _pinned.UnionWith(names);

    internal void Unknown() => _unknown = true;

    internal void Access(IEnumerable<string> names) => _accessed.UnionWith(names);

    internal void Inherits(string fullName)
    {
        var inherited = FrameworkNameCatalog.Current.Inherited(fullName, project);
        if (inherited is null)
        {
            _unknown = true;
            return;
        }

        Scope(inherited.NestedTypes, inherited.Members);
    }

    /// <summary>
    /// Rewrites a compilation unit.
    /// </summary>
    /// <param name="source">The source, with at most one file-scoped namespace and no using directives of its own.</param>
    /// <param name="authored">The spans of authored text, which keep their bytes.</param>
    /// <param name="required">The namespaces the renderer asked to import.</param>
    /// <returns>The rewritten source, its using directives in place.</returns>
    internal string Rewrite(string source, IReadOnlyList<TextSpan> authored, IEnumerable<string> required)
    {
        var tree = CSharpSyntaxTree.ParseText(SourceText.From(source), _parseOptions);
        var root = tree.GetCompilationUnitRoot();
        var namespaces = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().ToArray();
        var fileNamespace = namespaces.Length == 1 ? namespaces[0] as FileScopedNamespaceDeclarationSyntax : null;
        var name = fileNamespace is null ? string.Empty : fileNamespace.Name.ToString();
        IEnumerable<MemberDeclarationSyntax> members = fileNamespace is null ? root.Members : [.. root.Members, .. fileNamespace.Members];
        var declared = members.OfType<BaseTypeDeclarationSyntax>()
            .Select(type => new GeneratedTypeName(name, type.Identifier.ValueText, type is TypeDeclarationSyntax { TypeParameterList: { } parameters } ? parameters.Parameters.Count : 0));
        var inventory = generated.Concat(declared).Distinct().ToArray();
        var references = new QualifiedReferences(root, inventory, project, authored).Find();
        var scope = FileScope.Read(root, name, inventory, project, [.. references.Select(reference => reference.Span)]);
        var unknown = _unknown || scope.IsUnknown || (namespaces.Length > 0 && fileNamespace is null) || root.Usings.Count > 0 ||
            tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ||
            root.DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.IfDirectiveTrivia));
        var resolution = ImportResolver.Resolve(new ImportRequest(name, project, inventory, references.Select(reference => reference.Type))
        {
            RequiredImports = required,
            ScopeTypeNames = [.. scope.TypeNames, .. _types],
            ScopeValueNames = [.. scope.ValueNames, .. _values],
            PinnedNames = [.. scope.PinnedNames, .. _pinned],
            MemberAccessNames = [.. scope.MemberAccessNames, .. _accessed],
            HasUnknownScope = unknown,
            HasScaffoldGlobalUsings = hasScaffoldGlobalUsings
        });

        var changes = new List<TextChange>();
        foreach (var reference in references)
        {
            var spelling = resolution.Spell(reference.Type);
            if (spelling != reference.Type.Qualified) changes.Add(new(reference.Span, spelling));
        }

        if (fileNamespace is not null && resolution.Usings.Length > 0)
        {
            changes.Add(new(new(fileNamespace.SemicolonToken.Span.End, 0), string.Concat(resolution.Usings.Select(@namespace => $"{Environment.NewLine}using {resolution.Inner(@namespace)};")).Insert(0, Environment.NewLine)));
        }

        var text = new StringBuilder();
        foreach (var @using in resolution.TopLevelUsings) text.Append("using ").Append(@using).Append(';').AppendLine();
        if (text.Length > 0) text.AppendLine();
        return text.Append(SourceText.From(source).WithChanges(changes.OrderBy(change => change.Span.Start))).ToString();
    }
}
