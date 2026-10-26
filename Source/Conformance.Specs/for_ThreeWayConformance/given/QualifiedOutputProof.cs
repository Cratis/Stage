// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// Proves that the import resolver's output binds exactly like today's fully qualified output. Each file is rewritten
/// through the resolver, both versions are compiled the way the scaffolded projects compile them — the application with
/// its global usings, the <c language="csharp">when_*.cs</c> specifications in a second compilation with theirs, referencing
/// the first — and every name, invocation, attribute and pattern-bound member must bind to the same symbol, with the same
/// compiler errors, before and after.
/// </summary>
static class QualifiedOutputProof
{
    static readonly Lazy<(ImmutableArray<MetadataReference> Application, ImmutableArray<MetadataReference> Specifications)> _references = new(() =>
    {
        var projects = Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog.given.a_restored_scaffold.Projects().GetAwaiter().GetResult();
        return (References(projects.Application), References(projects.Specifications));
    });

    static readonly CSharpParseOptions _parseOptions = new(documentationMode: DocumentationMode.Diagnose, preprocessorSymbols: ["DEBUG"]);
    static readonly CSharpCompilationOptions _options = new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);
    static ImmutableArray<MetadataReference> References(ScaffoldedProject project) =>
        [.. project.References.Select(reference => reference.Path).Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path))];

    /// <summary>
    /// Rewrites rendered files through the resolver and compares their bindings.
    /// </summary>
    /// <param name="files">Today's rendered files.</param>
    /// <param name="inventory">The generated type inventory admission computes, or <see langword="null"/> to use the declarations of the files themselves.</param>
    /// <returns>The outcome.</returns>
    public static QualifiedOutputResult Verify(IEnumerable<RenderedFile> files, IEnumerable<GeneratedTypeName>? inventory = null)
    {
        // A rejected row can render two files to the same path; keep each one a tree of its own.
        var given = files.ToArray();
        var all = given.Select((file, index) => given.Take(index).Any(earlier => earlier.RelativePath == file.RelativePath)
            ? file with { RelativePath = $"duplicate{index}~/{file.RelativePath}" }
            : file).ToArray();
        var original = Compile(all);
        var failures = new List<string>();
        var applicationTypes = Declarations(original.Application).ToHashSet();
        if (inventory is not null)
        {
            var known = inventory.ToHashSet();
            failures.AddRange(applicationTypes.Where(type => !known.Contains(type)).Select(type => $"The generated type inventory misses {type}."));
            applicationTypes.UnionWith(known);
        }

        applicationTypes.Add(new(string.Empty, "Program"));
        var specificationTypes = applicationTypes.Concat(Declarations(original.Specifications)).ToArray();
        var changes = new Dictionary<string, ImmutableArray<TextChange>>(StringComparer.Ordinal);
        var shortSpellings = 0;
        foreach (var (compilation, project, types) in new[]
        {
            (original.Application, GeneratedProjects.Application, (IEnumerable<GeneratedTypeName>)applicationTypes),
            (original.Specifications, GeneratedProjects.Specifications, specificationTypes)
        })
        {
            foreach (var tree in Rendered(compilation))
            {
                var (fileChanges, spelled) = QualifiedFileRewrite.Rewrite(compilation.GetSemanticModel(tree), project, types);
                shortSpellings += spelled;
                changes[tree.FilePath] = fileChanges;
            }
        }

        failures.AddRange(Compare(original, all, changes));
        var rewritten = all.Select((file, index) => given[index] with { Content = SourceText.From(file.Content).WithChanges(changes[file.RelativePath]).ToString() }).ToArray();
        return new(failures, shortSpellings, rewritten);
    }

    /// <summary>
    /// Compares actual renderer output with the qualified spelling oracle.
    /// </summary>
    /// <param name="qualified">The pre-short-name output.</param>
    /// <param name="rendered">The real short-name output.</param>
    /// <param name="allowShortFailure">Whether a hostile addition may make only the short version fail compilation.</param>
    /// <returns>The proof outcome.</returns>
    internal static QualifiedOutputResult VerifyRendered(IReadOnlyList<RenderedFile> qualified, IReadOnlyList<RenderedFile> rendered, bool allowShortFailure = false)
    {
        var original = Compile(qualified);
        var actual = Compile(rendered);
        var failures = new List<string>();
        var shortSpellings = 0;
        foreach (var (before, after) in new[] { (original.Application, actual.Application), (original.Specifications, actual.Specifications) })
        {
            foreach (var tree in Rendered(before))
            {
                var counterpart = after.SyntaxTrees.Single(candidate => candidate.FilePath == tree.FilePath);
                var errorsBefore = Errors(before, tree);
                var errorsAfter = Errors(after, counterpart);
                if (allowShortFailure && errorsBefore.Length == 0 && errorsAfter.Length > 0) continue;
                if (errorsBefore != errorsAfter) failures.Add($"{tree.FilePath}: compiler errors changed from [{errorsBefore}] to [{errorsAfter}]");
                failures.AddRange(BindingSites.RenderedDifferences(before.GetSemanticModel(tree), after.GetSemanticModel(counterpart)));
                shortSpellings += QualifiedReferences(tree) - QualifiedReferences(counterpart);
            }
        }

        return new(failures, shortSpellings, rendered);
    }

    /// <summary>
    /// Compares the bindings and compiler errors of files before and after text changes.
    /// </summary>
    /// <param name="files">The original files.</param>
    /// <param name="changes">The changes to each file, keyed by its relative path.</param>
    /// <returns>Every binding or compiler error that differs.</returns>
    public static IReadOnlyList<string> Compare(IReadOnlyList<RenderedFile> files, IReadOnlyDictionary<string, ImmutableArray<TextChange>> changes) =>
        Compare(Compile(files), files, changes);

    static IReadOnlyList<string> Compare(
        (CSharpCompilation Application, CSharpCompilation Specifications) original,
        IReadOnlyList<RenderedFile> files,
        IReadOnlyDictionary<string, ImmutableArray<TextChange>> changes)
    {
        var after = Compile([.. files.Select(file => file with { Content = SourceText.From(file.Content).WithChanges(changes.GetValueOrDefault(file.RelativePath, [])).ToString() })]);
        var failures = new List<string>();
        foreach (var (before, compiled) in new[] { (original.Application, after.Application), (original.Specifications, after.Specifications) })
        {
            foreach (var tree in Rendered(before))
            {
                var counterpart = compiled.SyntaxTrees.Single(candidate => candidate.FilePath == tree.FilePath);
                failures.AddRange(BindingSites.Differences(before.GetSemanticModel(tree), compiled.GetSemanticModel(counterpart), changes.GetValueOrDefault(tree.FilePath, [])));
                var errorsBefore = Errors(before, tree);
                var errorsAfter = Errors(compiled, counterpart);
                if (errorsBefore != errorsAfter) failures.Add($"{tree.FilePath}: compiler errors changed from [{errorsBefore}] to [{errorsAfter}]");
            }
        }

        return failures;
    }

    static (CSharpCompilation Application, CSharpCompilation Specifications) Compile(IReadOnlyList<RenderedFile> files)
    {
        var application = CSharpCompilation.Create(
            $"RenderedApplication_{Guid.NewGuid():N}",
            [GlobalUsings(GeneratedProjects.Application), Parse("Specifications.g.cs", "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"RenderedSpecifications\")]"),
                .. files.Where(file => !IsSpecification(file)).Select(file => Parse(file.RelativePath, file.Content))],
            _references.Value.Application,
            _options);
        var specifications = CSharpCompilation.Create(
            "RenderedSpecifications",
            [GlobalUsings(GeneratedProjects.Specifications), .. files.Where(IsSpecification).Select(file => Parse(file.RelativePath, file.Content))],
            [.. _references.Value.Specifications, application.ToMetadataReference()],
            _options);
        return (application, specifications);
    }

    /// <summary>
    /// Measures how short a rendered application is spelled: its compiler errors, its unused or duplicate usings, its
    /// remaining <c language="csharp">global::</c> references and why each file kept its scope unknown, if it did.
    /// </summary>
    /// <param name="files">The rendered sources.</param>
    /// <param name="qualified">The same application rendered in the fully qualified spelling, which is what the import rewrite reads to decide whether the scope is known.</param>
    /// <returns>The measurement.</returns>
    internal static RenderedMeasurement Measure(IReadOnlyList<RenderedFile> files, IReadOnlyList<RenderedFile> qualified)
    {
        var qualifiedByPath = qualified.GroupBy(file => file.RelativePath).ToDictionary(group => group.Key, group => group.First().Content, StringComparer.Ordinal);
        var compiled = Compile(files);
        var compilations = new[] { (compiled.Application, GeneratedProjects.Application), (compiled.Specifications, GeneratedProjects.Specifications) };
        var inventory = compilations.SelectMany(entry => Declarations(entry.Item1)).Append(new(string.Empty, "Program")).Distinct().ToArray();
        List<string> errors = [];
        List<string> unused = [];
        List<ResidualQualification> residuals = [];
        Dictionary<string, UnknownScopeReason> reasons = new(StringComparer.Ordinal);
        foreach (var (compilation, project) in compilations)
        {
            errors.AddRange(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.ToString()));
            foreach (var tree in Rendered(compilation))
            {
                reasons[tree.FilePath] = new CSharpImports(project, inventory).Classify(WithoutUsings(qualifiedByPath.GetValueOrDefault(tree.FilePath, tree.ToString())), []);
                unused.AddRange(compilation.GetSemanticModel(tree).GetDiagnostics().Where(diagnostic => diagnostic.Id is "CS8019" or "CS0105")
                    .Select(diagnostic => $"{tree.FilePath}: {diagnostic.Id} {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}"));
                residuals.AddRange(ResidualQualifications(compilation.GetSemanticModel(tree), tree));
            }
        }

        return new(errors, unused, residuals, reasons);
    }

    /// <summary>
    /// Lists the fully qualified type names a file still spells with <c language="csharp">global::</c>.
    /// </summary>
    /// <param name="model">The semantic model of the file.</param>
    /// <param name="tree">The file.</param>
    /// <returns>Each residual reference.</returns>
    static IEnumerable<ResidualQualification> ResidualQualifications(SemanticModel model, SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var declared = root.DescendantNodes().Select(node => model.GetDeclaredSymbol(node)).OfType<ISymbol>()
            .Where(symbol => symbol is IFieldSymbol or IPropertySymbol or IParameterSymbol or ILocalSymbol or IMethodSymbol or IEventSymbol)
            .Select(symbol => symbol.Name).ToHashSet(StringComparer.Ordinal);
        var referenced = root.DescendantNodes().OfType<SimpleNameSyntax>()
            .Select(name => model.GetSymbolInfo(name).Symbol).OfType<INamedTypeSymbol>().Select(symbol => symbol.OriginalDefinition).Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        foreach (var alias in root.DescendantNodes().OfType<AliasQualifiedNameSyntax>().Where(name => !name.Ancestors().OfType<UsingDirectiveSyntax>().Any()))
        {
            SyntaxNode node = alias;
            INamedTypeSymbol? type = null;
            while (true)
            {
                type = model.GetSymbolInfo(node).Symbol as INamedTypeSymbol ?? model.GetTypeInfo(node).Type as INamedTypeSymbol;
                if (type is not null) break;
                var parent = node.Parent;
                if (parent is QualifiedNameSyntax qualified && qualified.Left == node) node = qualified;
                else if (parent is MemberAccessExpressionSyntax access && access.Expression == node) node = access;
                else break;
            }

            var simple = type?.Name ?? node.ToString();
            var collides = declared.Contains(simple) ||
                referenced.Count(candidate => candidate.Name == simple) > 1 ||
                model.Compilation.GlobalNamespace.GetNamespaceMembers().Any(@namespace => @namespace.Name == simple);
            yield return new(tree.FilePath, type?.ContainingNamespace.ToDisplayString() ?? string.Empty, simple, node.ToString(), collides);
        }
    }

    static string WithoutUsings(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source, _parseOptions).GetCompilationUnitRoot();
        var stripped = root.RemoveNodes(root.DescendantNodes().OfType<UsingDirectiveSyntax>(), SyntaxRemoveOptions.KeepNoTrivia) ?? root;
        return stripped.ToFullString();
    }

    /// <summary>
    /// Requires an independently rendered proof fixture to compile, not merely to reproduce the same errors.
    /// </summary>
    /// <param name="files">The rendered sources.</param>
    /// <returns>The compilation errors from both scaffold projects.</returns>
    internal static IReadOnlyList<string> CompilationErrors(IReadOnlyList<RenderedFile> files)
    {
        var compiled = Compile(files);
        return [.. new[] { compiled.Application, compiled.Specifications }.SelectMany(compilation => compilation.GetDiagnostics())
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.ToString())];
    }

    static int QualifiedReferences(SyntaxTree tree) => tree.GetRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>()
        .Count(name => !name.Ancestors().OfType<UsingDirectiveSyntax>().Any());

    static bool IsSpecification(RenderedFile file) => Path.GetFileName(file.RelativePath).StartsWith("when_", StringComparison.Ordinal);

    static IEnumerable<SyntaxTree> Rendered(CSharpCompilation compilation) => compilation.SyntaxTrees.Where(tree => !tree.FilePath.EndsWith(".g.cs", StringComparison.Ordinal));

    static SyntaxTree Parse(string path, string content) => CSharpSyntaxTree.ParseText(SourceText.From(content), _parseOptions, path);

    // The complete scaffold reference set resolves every implicit and package global namespace.
    static SyntaxTree GlobalUsings(GeneratedProjects project) => Parse(
        $"GlobalUsings.{project}.g.cs",
        string.Concat(FrameworkNameCatalog.Current.GlobalUsings(project).Select(@namespace => $"global using {@namespace};\n")));

    static string Errors(CSharpCompilation compilation, SyntaxTree tree) => string.Join(", ", compilation.GetSemanticModel(tree).GetDiagnostics()
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));

    static IEnumerable<GeneratedTypeName> Declarations(CSharpCompilation compilation) => Rendered(compilation)
        .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
        .Where(declaration => declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax && declaration.Parent is not TypeDeclarationSyntax)
        .Select(declaration => new GeneratedTypeName(
            string.Join('.', declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(@namespace => @namespace.Name.ToString())),
            declaration switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
                DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
                _ => string.Empty
            },
            declaration switch
            {
                TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Count ?? 0,
                DelegateDeclarationSyntax @delegate => @delegate.TypeParameterList?.Parameters.Count ?? 0,
                _ => 0
            }));
}

/// <summary>
/// A type a rendered file still spells with <c language="csharp">global::</c>.
/// </summary>
/// <param name="Path">The file.</param>
/// <param name="Namespace">The namespace of the type, or empty when it did not resolve to a type.</param>
/// <param name="Name">The simple name of the type.</param>
/// <param name="Text">The qualified text as written.</param>
/// <param name="Collides">Whether the simple name is also another type, a member or local, or a root namespace in that compilation.</param>
sealed record ResidualQualification(string Path, string Namespace, string Name, string Text, bool Collides);

/// <summary>
/// How short a rendered application is spelled.
/// </summary>
/// <param name="CompilationErrors">Every compiler error of both scaffold projects.</param>
/// <param name="UnusedUsings">Every unused or duplicate using directive.</param>
/// <param name="Residuals">Every remaining <c language="csharp">global::</c> type reference.</param>
/// <param name="UnknownScope">Why a file kept every type qualified, by path.</param>
sealed record RenderedMeasurement(
    IReadOnlyList<string> CompilationErrors,
    IReadOnlyList<string> UnusedUsings,
    IReadOnlyList<ResidualQualification> Residuals,
    IReadOnlyDictionary<string, UnknownScopeReason> UnknownScope);
#endif
