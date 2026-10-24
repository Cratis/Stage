// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

static class ArtifactDependencyAdmission
{
    internal static List<PlannedArtifact> Admit(
        IReadOnlyList<PlannedArtifact> original,
        List<PlannedArtifact> admitted,
        SemanticId application,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        var omitted = original.Except(admitted).ToHashSet();
        if (omitted.Count == 0) return admitted;

        var sources = original.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .ToDictionary(artifact => artifact, artifact => CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var compilation = CSharpCompilation.Create("RenderingDependencies", sources.Values);
        var graph = new DependencyGraph(
            sources,
            sources.ToDictionary(pair => pair.Key, pair => Dependencies(compilation, pair.Value)),
            sources.ToDictionary(pair => pair.Key, pair => Declarations(pair.Value)),
            sources.ToDictionary(pair => pair.Key, pair => UnboundNames(compilation, pair.Value)));
        var causes = original.ToDictionary(artifact => artifact, artifact => diagnostics.FirstOrDefault(diagnostic =>
            IsPathOmission(diagnostic) && artifact.Sources.Contains(diagnostic.Artifact)));
        var fallback = diagnostics.First(IsPathOmission);

        while (true)
        {
            var rejected = Rejected(graph, omitted, admitted, application);
            if (rejected.Length == 0) return admitted;

            foreach (var artifact in rejected)
            {
                var cause = omitted.Select(removed => causes[removed]).FirstOrDefault(diagnostic => diagnostic is not null) ?? fallback;
                causes[artifact] = cause;
                omitted.Add(artifact);
                admitted.Remove(artifact);
                Warn(artifact, cause, application, diagnostics);
            }
        }
    }

    static PlannedArtifact[] Rejected(DependencyGraph graph, HashSet<PlannedArtifact> omitted, List<PlannedArtifact> admitted, SemanticId application)
    {
        var owners = omitted.SelectMany(artifact => artifact.Sources).Where(owner => owner != application).ToHashSet();
        var removedTrees = omitted.Where(graph.Sources.ContainsKey).Select(artifact => graph.Sources[artifact]).ToHashSet();
        var missingNamespaces = MissingNamespaces(graph.Sources, omitted);

        // Name matching is intentionally conservative when missing framework references prevent Roslyn binding.
        var missingNames = graph.Declarations.Where(pair => omitted.Contains(pair.Key)).SelectMany(pair => pair.Value)
            .Except(graph.Declarations.Where(pair => !omitted.Contains(pair.Key)).SelectMany(pair => pair.Value), StringComparer.Ordinal)
            .Select(name => name[(name.LastIndexOf('.') + 1)..]).ToHashSet(StringComparer.Ordinal);
        return [.. admitted.Where(artifact => artifact.Sources.Any(owners.Contains) ||
            (graph.Sources.TryGetValue(artifact, out var tree) &&
            (graph.References[artifact].Any(references => references.All(removedTrees.Contains)) ||
                ReferencesMissingName(tree, graph.UnboundNames[artifact], missingNames, missingNamespaces))))];
    }

    static bool IsPathOmission(ArtifactRenderDiagnostic diagnostic) =>
        string.Equals(diagnostic.Code, "STAGE-CRATIS-004", StringComparison.Ordinal) || string.Equals(diagnostic.Code, "STAGE-CRATIS-005", StringComparison.Ordinal);

    static void Warn(PlannedArtifact artifact, ArtifactRenderDiagnostic cause, SemanticId application, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var owner in artifact.Sources.DefaultIfEmpty(application))
        {
            diagnostics.Add(new(
                cause.Code,
                ArtifactRenderDiagnosticSeverity.Warning,
                $"Generated artifact '{artifact.RelativePath}' is omitted because it depends on an omitted rendering owner. {cause.Message}",
                owner));
        }
    }

    static SyntaxTree[][] Dependencies(CSharpCompilation compilation, SyntaxTree tree)
    {
        var model = compilation.GetSemanticModel(tree);
        return [.. tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>()
            .SelectMany(name => Symbols(model.GetSymbolInfo(name)))
            .Where(symbol => symbol is not INamespaceSymbol)
            .Select(symbol => symbol.DeclaringSyntaxReferences.Select(reference => reference.SyntaxTree).Distinct().ToArray())
            .Where(references => references.Length > 0)];
    }

    static ImmutableArray<ISymbol> Symbols(SymbolInfo info) => info.Symbol is { } symbol ? [symbol] : info.CandidateSymbols;

    static string[] Declarations(SyntaxTree tree) =>
        [.. tree.GetRoot().DescendantNodes().Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            .Select(node => $"{Namespace(node)}.{(node is BaseTypeDeclarationSyntax type ? type.Identifier.ValueText : ((DelegateDeclarationSyntax)node).Identifier.ValueText)}")];

    static string Namespace(SyntaxNode node) => string.Join('.', node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(declaration => declaration.Name.ToString()));

    static HashSet<string> MissingNamespaces(Dictionary<PlannedArtifact, SyntaxTree> sources, HashSet<PlannedArtifact> omitted)
    {
        var retained = sources.Where(pair => !omitted.Contains(pair.Key)).SelectMany(pair => Namespaces(pair.Value)).ToHashSet(StringComparer.Ordinal);
        return sources.Where(pair => omitted.Contains(pair.Key)).SelectMany(pair => Namespaces(pair.Value)).Except(retained, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
    }

    static IEnumerable<string> Namespaces(SyntaxTree tree) => tree.GetRoot().DescendantNodes().Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
        .SelectMany(type =>
        {
            var parts = Namespace(type).Split('.');
            return Enumerable.Range(1, parts.Length).Select(length => string.Join('.', parts.Take(length)));
        });

    static HashSet<string> UnboundNames(CSharpCompilation compilation, SyntaxTree tree)
    {
        var model = compilation.GetSemanticModel(tree);
        return tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>()
            .Where(name => model.GetSymbolInfo(name) is { Symbol: null, CandidateSymbols.IsEmpty: true })
            .Select(name => name.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
    }

    static bool ReferencesMissingName(SyntaxTree tree, HashSet<string> unbound, HashSet<string> names, HashSet<string> namespaces) =>
        unbound.Any(name => names.Contains(name) || names.Contains(name + "Attribute")) ||
        tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Any(directive => directive.Name is { } name && namespaces.Contains(name.ToString().Replace("global::", string.Empty, StringComparison.Ordinal)));

    sealed record DependencyGraph(
        Dictionary<PlannedArtifact, SyntaxTree> Sources,
        Dictionary<PlannedArtifact, SyntaxTree[][]> References,
        Dictionary<PlannedArtifact, string[]> Declarations,
        Dictionary<PlannedArtifact, HashSet<string>> UnboundNames);
}
