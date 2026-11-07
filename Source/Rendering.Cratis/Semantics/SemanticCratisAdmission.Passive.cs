// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidatePassiveReadModels(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (context.PassiveReadModels.IsEmpty) return;
        foreach (var projection in slices.SelectMany(located => located.Slice.Projections).Where(projection => context.IsPassive(projection.ReadModel)))
        {
            var reason = PassiveReplayRejection(projection.Scope);
            if (reason is not null)
            {
                diagnostics.Add(Error(
                    SemanticVersionFeatures.PassiveReadModelReplay,
                    $"Passive read model '{context.ReadModels[projection.ReadModel].Name}' cannot be answered by replaying the requested key's events; {reason}",
                    projection.ReadModel));
            }
        }
    }

    static string? PassiveReplayRejection(SemanticProjectionScope? root)
    {
        if (root?.From.Any(from => !IsEventSourceKey(from.Key)) != false || root.Removals.Any(removal => !IsEventSourceKey(removal.Key)))
            return "the root key must be the event-source id, not a payload, constant, composite or context key (CHR0046).";
        if (!root.Joins.IsEmpty || !root.JoinRemovals.IsEmpty)
            return "joins can require events from other event-source streams.";
        if (!root.Children.IsEmpty || !root.Nested.IsEmpty)
            return "child and nested scopes can require events from other event-source streams.";
        return null;
    }

    static bool IsEventSourceKey(SemanticProjectionKey key) => key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity };
}
