// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

sealed class ArtifactDependencyAdmission(
    SemanticApplication application,
    List<ArtifactRenderDiagnostic> diagnostics,
    RenderingPartAdmission? parts = null)
{
    readonly Lazy<HashSet<SemanticId>> _slices = new(() => [.. GeneratedTypeNames.AllSlices(application).Select(located => located.Slice.Id)]);
    readonly List<ArtifactRenderDiagnostic> _provenance = [];
    readonly Lazy<HashSet<SemanticId>> _specifications = new(() => [.. GeneratedTypeNames.AllSlices(application).SelectMany(located => located.Slice.Specifications).Select(specification => specification.Id)]);

    internal List<PlannedArtifact> Admit(
        IReadOnlyList<PlannedArtifact> original,
        List<PlannedArtifact> admitted)
    {
        admitted = AdmitDependents(original, admitted);
        AdmitOmittedProvenance();
        return admitted;
    }

    static PlannedArtifact[] Rejected(DependencyGraph graph, HashSet<PlannedArtifact> omitted, List<PlannedArtifact> admitted, SemanticApplication application)
    {
        var slices = GeneratedTypeNames.AllSlices(application).Select(located => located.Slice.Id).ToHashSet();

        // A specification file also cites its enclosing slice for provenance. Losing that file must
        // not evict an independent command merely because it shares the enclosing slice.
        var owners = omitted.SelectMany(artifact => artifact.Sources.Where(owner =>
            owner != application.Id && (!slices.Contains(owner) || artifact.Sources.Length == 1))).ToHashSet();
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

    List<PlannedArtifact> AdmitDependents(
        IReadOnlyList<PlannedArtifact> original,
        List<PlannedArtifact> admitted)
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
        var omissions = diagnostics.Where(diagnostic => IsOmission(diagnostic, omitted)).ToArray();
        var causes = original.ToDictionary(artifact => artifact, artifact => omissions.FirstOrDefault(diagnostic =>
            artifact.Sources.Contains(diagnostic.Artifact)));
        var fallback = omissions[0];

        // Each pass removes at least one artifact; slice retention can only remove more.
        for (var remaining = admitted.Count; remaining > 0; remaining--)
        {
            admitted = parts?.Retained(admitted) ?? admitted;
            omitted.UnionWith(original.Except(admitted));
            var rejected = Rejected(graph, omitted, admitted, application);
            if (rejected.Length == 0) return admitted;

            foreach (var artifact in rejected)
            {
                var cause = omitted.Select(removed => causes[removed]).FirstOrDefault(diagnostic => diagnostic is not null) ?? fallback;
                causes[artifact] = cause;
                omitted.Add(artifact);
                admitted.Remove(artifact);
                Warn(artifact, cause);
            }
        }
        return admitted;
    }

    bool IsOmission(ArtifactRenderDiagnostic diagnostic, HashSet<PlannedArtifact> omitted) =>
        diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning &&
        ((parts?.IsGap(diagnostic) == true && parts.IsOmitted(diagnostic.Artifact)) ||
            (RenderingGapReports.IsPathDiagnostic(diagnostic) && omitted.Any(artifact => artifact.Sources.Contains(diagnostic.Artifact))));

    void Warn(PlannedArtifact artifact, ArtifactRenderDiagnostic cause)
    {
        var warnings = artifact.Sources.DefaultIfEmpty(application.Id).Select(owner => new ArtifactRenderDiagnostic(
            cause.Code,
            ArtifactRenderDiagnosticSeverity.Warning,
            $"Generated artifact '{artifact.RelativePath}' is omitted because it depends on an omitted rendering owner. {cause.Message}",
            owner)).ToArray();
        diagnostics.AddRange(warnings);
        if (parts is null) return;

        foreach (var warning in warnings.Where(warning => !IsProvenance(artifact, warning.Artifact)))
        {
            parts.AdmitDependencyOmission(warning, !_specifications.Value.Contains(warning.Artifact));
        }
        _provenance.AddRange(warnings.Where(warning => IsProvenance(artifact, warning.Artifact)));
    }

    void AdmitOmittedProvenance()
    {
        foreach (var warning in _provenance.Where(warning => parts?.IsOmitted(warning.Artifact) == true))
        {
            parts!.AdmitDependencyOmission(warning, false);
        }
    }

    // Enclosing-slice provenance on a multi-source file is a gap only when another owner's omission
    // took the slice with it; on its own it must not evict the slice's independent parts.
    // A specification is a leaf: losing its file is a gap for the specification, but must not
    // evict the slice it verifies.
    bool IsProvenance(PlannedArtifact artifact, SemanticId owner) =>
        artifact.Sources.Length > 1 && _slices.Value.Contains(owner);

    sealed record DependencyGraph(
        Dictionary<PlannedArtifact, SyntaxTree> Sources,
        Dictionary<PlannedArtifact, SyntaxTree[][]> References,
        Dictionary<PlannedArtifact, string[]> Declarations,
        Dictionary<PlannedArtifact, HashSet<string>> UnboundNames);
}
