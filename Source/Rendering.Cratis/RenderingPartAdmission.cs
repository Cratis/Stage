// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Constraints;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Rendering.Cratis;

sealed class RenderingPartAdmission(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
{
    readonly SemanticOwnerIndex _owners = new(context.Application);
    readonly HashSet<SemanticId> _omitted = [];
    readonly HashSet<(SemanticId Artifact, string Code)> _gaps = [];

    internal ImmutableArray<ArtifactRenderRefusal> Refusals { get; private set; } = [];

    internal ImmutableArray<ArtifactRemoval> Removals { get; private set; } = [];

    internal bool HasUncontainedErrors => diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
        (!_owners.Contains(diagnostic.Artifact) || !Refusals.Any(refusal => refusal.Artifact == diagnostic.Artifact && refusal.Codes.Contains(diagnostic.Code))));

    internal List<RenderedFile> RenderModelParts(IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var files = RenderSharedParts(slices);
        if (slices.Any(slice => slice.Slice.Commands.Any(command => command.Authorization is not null) ||
            slice.Slice.Queries.Any(query => query.Authorization is not null)))
        {
            var policies = slices.SelectMany(located => Render(
                located.Slice.Id,
                () => SemanticPolicyArtifactRenderer.Render(context, [located]).Concat(SemanticPolicyArtifactRenderer.RenderOpaque(context, [located]))));
            files.AddRange(policies.Select(AdmittedPolicySources).DistinctBy(file => (file.RelativePath, file.Content)));
        }
        foreach (var located in slices)
        {
            files.AddRange(Render(located.Slice.Id, () => RenderSlice(located)));
        }
        foreach (var constraint in SemanticCratisAdmission.SelectedConstraints(context, slices))
        {
            files.AddRange(Render(
                constraint.Slice.Id,
                () => [SemanticConstraintArtifactRenderer.Render(constraint.Slice, constraint.Constraint, context)]));
        }
        return files;
    }

    internal bool IsOmitted(SemanticId owner) => _omitted.Contains(owner);

    internal void AdmitDependencyOmission(ArtifactRenderDiagnostic diagnostic, bool omitOwner)
    {
        _gaps.Add((diagnostic.Artifact, diagnostic.Code));

        // Application provenance on a dependent file is not a refusal of the application shell.
        if (omitOwner && diagnostic.Artifact != context.Application.Id && _owners.Contains(diagnostic.Artifact))
        {
            _omitted.UnionWith(_owners.Omissions(diagnostic.Artifact));
        }
    }

    internal SemanticApplicationContext SceneContext(CratisRenderingOptions options)
    {
        var commands = context.Commands.Values.Where(command => command.Properties.Any(property => IsOmittedType(property.Type))).Select(command => command.Id);
        var queries = context.Queries.Values.Where(HasOmittedQueryType).Select(query => query.Id);
        return new(context.Request, options, _omitted.Concat(commands).Concat(queries).ToHashSet());
    }

    internal bool IsGap(ArtifactRenderDiagnostic diagnostic) =>
        diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && _gaps.Contains((diagnostic.Artifact, diagnostic.Code));

    internal List<RenderedFile> Render(SemanticId owner, Func<IEnumerable<RenderedFile>> render)
    {
        if (_omitted.Contains(owner)) return [];
        try
        {
            // Materialize before publishing: a deferred renderer can fail after yielding its first file.
            return [.. render()];
        }
        catch (InvalidTypedContext exception)
        {
            Reject(new("STAGE-ESM-021", ArtifactRenderDiagnosticSeverity.Warning, exception.Message, owner));
        }
        catch (UnsupportedSemanticRendering exception)
        {
            Reject(new(exception.Code, ArtifactRenderDiagnosticSeverity.Warning, exception.Message, owner));
        }
        return [];
    }

    internal void AdmitDiagnostics(ImmutableArray<ArtifactRenderRefusal> refusals, ImmutableArray<ArtifactRemoval> removals)
    {
        Refusals = refusals;
        Removals = removals;
        var covered = refusals.SelectMany(refusal => refusal.Codes.Select(code => (refusal.Artifact, Code: code))).ToHashSet();
        for (var index = 0; index < diagnostics.Count; index++)
        {
            var diagnostic = diagnostics[index];
            if (covered.Contains((diagnostic.Artifact, diagnostic.Code))) continue;
            if (!CanAdmit(diagnostic)) continue;
            diagnostics[index] = diagnostic with { Severity = ArtifactRenderDiagnosticSeverity.Warning };
            AdmitGap(diagnostics[index]);
        }
    }

    internal ImmutableArray<ArtifactRemoval> AdmittedRemovals(IEnumerable<PlannedArtifact> artifacts)
    {
        if (Removals.IsDefault) return [];
        if (diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error)) return [];
        var realized = artifacts.SelectMany(artifact => artifact.Sources).ToHashSet();
        return [.. Removals.Where(removal => realized.Contains(removal.Model) && !IsOmitted(removal.Model))];
    }

    internal List<PlannedArtifact> Retained(List<PlannedArtifact> artifacts) =>
        [.. artifacts.Where(artifact => !artifact.Sources.Any(_omitted.Contains))];

    internal List<PlannedArtifact> MissingDeclarations()
    {
        // These dependency-analysis inputs are never published. They let the existing closure find
        // consumers even when the rejected producer could not emit a single file.
        var declarations = SemanticCratisAdmission.GeneratedDeclarations(context, context.AllSlices)
            .Where(declaration => _omitted.Contains(declaration.Artifact))
            .Select(declaration => (declaration.Artifact, Namespace: string.Join('.', new[] { context.RootNamespace, declaration.Namespace }.Where(part => part.Length > 0)), declaration.Name))
            .Concat(context.Request.TypedContextDescriptors.Where(descriptor => descriptor.OperationId is { } owner && _omitted.Contains(owner))
                .Select(descriptor => (Artifact: descriptor.OperationId.GetValueOrDefault(), Namespace: context.TypedContextsNamespace, Name: $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}")))
            .Distinct().ToArray();
        return [.. declarations.Select((declaration, index) => PlannedArtifact.CreateText(
            $".stage-omitted/{index}.cs",
            $"namespace {declaration.Namespace} {{ public class {declaration.Name} {{ }} }}",
            [declaration.Artifact]))];
    }

    bool CanAdmit(ArtifactRenderDiagnostic diagnostic) => _owners.Contains(diagnostic.Artifact) &&
        (diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error ||
            string.Equals(diagnostic.Code, Contracts.Scene.UnsupportedGuardedScreenAction.DiagnosticCode, StringComparison.Ordinal));

    void AdmitGap(ArtifactRenderDiagnostic diagnostic)
    {
        if (string.Equals(diagnostic.Code, Contracts.Scene.UnsupportedGuardedScreenAction.DiagnosticCode, StringComparison.Ordinal))
        {
            _gaps.Add((diagnostic.Artifact, diagnostic.Code));
        }
        else if (SemanticGenerationRendering.IsGenerationFailure(context, diagnostic))
        {
            _gaps.Add((diagnostic.Artifact, diagnostic.Code));
            context.GenerationFailures[diagnostic.Artifact] = diagnostic.Message;
        }
        else
        {
            Omit(diagnostic);
        }
    }

    RenderedFile AdmittedPolicySources(RenderedFile file)
    {
        // Shared policy bytes and names stay selection-independent. Refused operations must not
        // evict a shared policy still realized by an admitted operation.
        var admitted = file.Sources.Where(owner => !IsOmitted(owner)).ToImmutableArray();
        return admitted.IsEmpty ? file : file with { Sources = admitted };
    }

    bool IsOmittedType(SemanticTypeReference type) => type.Kind != SemanticTypeReferenceKind.Primitive && IsOmitted(type.Target);

    bool HasOmittedQueryType(SemanticKeyedQuery query) =>
        (query.Argument is { } argument && IsOmittedType(argument.Type)) ||
        context.ReadModels[query.ReadModel].Properties.Any(property => IsOmittedType(property.Type));

    List<RenderedFile> RenderSharedParts(IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var shared = context.Request.Scope.Kind == ArtifactRenderScopeKind.Application
            ? (context.Application.Concepts, context.Application.Types)
            : SemanticSharedReferences.Collect(context, slices);
        var files = new List<RenderedFile>();
        foreach (var concept in shared.Concepts)
        {
            files.AddRange(Render(concept.Id, () => [SemanticCommonArtifactRenderer.Render(concept, context)]));
        }
        foreach (var type in shared.Types)
        {
            files.AddRange(Render(type.Id, () => [SemanticCommonArtifactRenderer.Render(type, context)]));
        }
        foreach (var source in SemanticEventSourceArtifactRenderer.Selected(context, slices))
        {
            files.AddRange(Render(source.Id, () => [SemanticEventSourceArtifactRenderer.Render(source, context)]));
        }
        return files;
    }

    IEnumerable<RenderedFile> RenderSlice(LocatedSemanticSlice located)
    {
        var renderer = SemanticSliceArtifactRenderers.Ordered.FirstOrDefault(candidate => candidate.Handles(located)) ??
            throw UnsupportedSemanticRendering.For(nameof(SemanticSliceKind), located.Slice.Kind);
        return renderer.Render(located, context);
    }

    void Reject(ArtifactRenderDiagnostic diagnostic)
    {
        if (!_owners.Contains(diagnostic.Artifact))
        {
            diagnostics.Add(diagnostic with { Severity = ArtifactRenderDiagnosticSeverity.Error });
            return;
        }
        diagnostics.Add(diagnostic);
        Omit(diagnostic);
    }

    void Omit(ArtifactRenderDiagnostic diagnostic)
    {
        _gaps.Add((diagnostic.Artifact, diagnostic.Code));
        _omitted.UnionWith(_owners.Omissions(diagnostic.Artifact));
    }
}
