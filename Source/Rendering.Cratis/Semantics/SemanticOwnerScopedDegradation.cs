// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Qualifies unsupported event members for graph-proven omission and describes each omitted owner.</summary>
internal static class SemanticOwnerScopedDegradation
{
    static readonly Dictionary<Type, string> _kinds = new()
    {
        [typeof(SemanticEventContract)] = "event",
        [typeof(SemanticCommand)] = "command",
        [typeof(SemanticReadModel)] = "readmodel",
        [typeof(SemanticProjection)] = "projection",
        [typeof(SemanticKeyedQuery)] = "query",
        [typeof(SemanticSpecification)] = "specification",
        [typeof(SemanticReaction)] = "reaction",
        [typeof(SemanticCapture)] = "capture",
        [typeof(SemanticSlice)] = "slice",
        [typeof(SemanticEventSource)] = "eventsource",
        [typeof(SemanticEventStream)] = "eventstream",
        [typeof(SemanticApplicationTrigger)] = "trigger",
        [typeof(SemanticConcept)] = "concept",
        [typeof(SemanticCompositeType)] = "type"
    };

    internal static IReadOnlyDictionary<SemanticId, string>? TryRoots(SemanticApplicationContext context, IEnumerable<ArtifactRenderDiagnostic> diagnostics, SemanticDependencyIndex index, Settings settings)
    {
        if (settings.Strict) return null;
        var errors = diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ToArray();
        var unsupported = errors.Where(diagnostic => diagnostic.Code == "STAGE-ESM-012").ToArray();
        if (!EligibleErrors(context, errors, unsupported)) return null;
        var roots = unsupported.Select(diagnostic => diagnostic.Artifact).Distinct().OrderBy(owner => owner.ToString(), StringComparer.Ordinal)
            .ToDictionary(owner => owner, _ => "STAGE-DEGRADE-001");
        var omission = SemanticOwnerOmission.Compute(context, index, roots);
        return EligibleClosure(context, omission, settings) ? roots : null;
    }

    internal static (ImmutableArray<ArtifactRenderDiagnostic> Warnings, ImmutableArray<ArtifactRenderDegradation> Degradations,
        ImmutableArray<ArtifactRenderRefusal> Refusals) Evidence(SemanticApplicationContext context, SemanticOwnerOmission omission)
    {
        var owners = Owners(context);
        var inventory = CratisSelectedSemanticOwners.Enumerate(context).Select(owner => owner.Id).ToHashSet();
        var warnings = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var degradations = ImmutableArray.CreateBuilder<ArtifactRenderDegradation>();
        var refusals = ImmutableArray.CreateBuilder<ArtifactRenderRefusal>();
        foreach (var identity in omission.OrderedClosure.OrderBy(owner => omission.Roots.ContainsKey(owner) ? 0 : 1))
        {
            var (kind, name) = owners[identity];
            var description = Describe(context, omission, identity, (kind, name));
            refusals.Add(new(identity, kind, [description.Code]));
            if (!inventory.Contains(identity)) continue;
            var warning = new ArtifactRenderDiagnostic(description.Code, ArtifactRenderDiagnosticSeverity.Warning, description.Message, identity);
            warnings.Add(warning);
            degradations.Add(new(warning, description.Remedy));
        }
        return (warnings.ToImmutable(), degradations.ToImmutable(), refusals.ToImmutable());
    }

    static bool EligibleErrors(SemanticApplicationContext context, ArtifactRenderDiagnostic[] errors, ArtifactRenderDiagnostic[] unsupported)
    {
        if (unsupported.Length == 0 || unsupported.Any(diagnostic => !IsEventFailure(context, diagnostic))) return false;
        var otherErrors = errors.Except(unsupported).ToArray();
        return otherErrors.Length == 0 || !SemanticRefusalScope.Classify(context, otherErrors).IsEmpty;
    }

    static bool IsEventFailure(SemanticApplicationContext context, ArtifactRenderDiagnostic diagnostic) =>
        context.OwnerScopedRefusals.Contains(diagnostic) && context.Events.ContainsKey(diagnostic.Artifact);

    static bool EligibleClosure(SemanticApplicationContext context, SemanticOwnerOmission? omission, Settings settings)
    {
        if (omission is null) return false;
        if (settings.Removals is { Count: > 0 } && omission.OrderedClosure.Any(context.ReadModels.ContainsKey)) return false;
        var owners = Owners(context);
        return omission.OrderedClosure.All(owners.ContainsKey);
    }

    static (string Code, string Message, string Remedy) Describe(SemanticApplicationContext context, SemanticOwnerOmission omission, SemanticId identity, (string Kind, string Name) owner)
    {
        if (omission.Roots.ContainsKey(identity))
        {
            var original = context.OwnerScopedRefusals.First(diagnostic => diagnostic.Artifact == identity).Message;
            return ("STAGE-DEGRADE-001", $"Omitted event '{owner.Name}' (STAGE-ESM-012): {original}",
                "Upgrade Stage to a version that supports this member, or change the event member to a supported type.");
        }
        var roots = string.Join(", ", omission.Causes[identity]);
        return ("STAGE-DEGRADE-005", $"Omitted {owner.Kind} '{owner.Name}' because it depends on an omitted owner. Caused by: {roots}",
            $"Resolve the root cause on {roots}.");
    }

    static Dictionary<SemanticId, (string Kind, string Name)> Owners(SemanticApplicationContext context)
    {
        var declarations = context.Concepts.Values.Cast<object>().Concat(context.Types.Values)
            .Concat(context.Application.EventSources).Concat(context.Application.EventSources.SelectMany(source => source.Streams))
            .Concat(context.Application.Triggers).Concat(context.AllSlices.SelectMany(located => InSlice(located.Slice)));
        return declarations.ToDictionary(owner => (SemanticId)owner.GetType().GetProperty("Id")!.GetValue(owner)!, owner => (_kinds[owner.GetType()], (string)owner.GetType().GetProperty("Name")!.GetValue(owner)!));
    }

    static IEnumerable<object> InSlice(SemanticSlice slice) => new object[] { slice }.Concat(slice.Commands).Concat(slice.Events)
        .Concat(slice.ReadModels).Concat(slice.Projections).Concat(slice.Queries).Concat(slice.Specifications).Concat(slice.Reactions).Concat(slice.Captures);

    internal sealed record Settings(bool Strict, IReadOnlyList<ArtifactRemoval>? Removals = null);
}
