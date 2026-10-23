// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Expresses a flat projection as the scope that says the same, so admission and the per-run projector see one lowering path.
/// </summary>
/// <remarks>
/// The rewrite depends on the plan alone and never on the specification, which keeps admission and the runtime in agreement.
/// A flat projection with no scoped form, or one whose transitions leave the read model identifier unmapped (the reference
/// refuses that), is returned unchanged and keeps the flat refusal.
/// </remarks>
internal static class SemanticRunFlatProjections
{
    /// <summary>
    /// Normalizes a read-model projection with flat transitions to its scoped form.
    /// </summary>
    /// <param name="projection">The projection from the plan.</param>
    /// <param name="readModel">The read model the projection builds.</param>
    /// <returns>The scoped equivalent, or the projection itself when it is not flat or has no scoped form.</returns>
    internal static SemanticProjection Normalize(SemanticProjection projection, SemanticReadModel readModel) =>
        projection is { Scope: null, Target: SemanticProjectionTargetKind.ReadModel, Transitions.Length: > 0 } &&
        MapsTheIdentifier(projection, readModel) &&
        SemanticFlatProjectionScopes.ToScope(projection) is { } scope
            ? projection with { Scope = scope, Transitions = [] }
            : projection;

    static bool MapsTheIdentifier(SemanticProjection projection, SemanticReadModel readModel)
    {
        var identifier = readModel.Properties.SingleOrDefault(property => property.IsIdentifier);
        return identifier is not null && projection.Transitions.All(transition => transition.Mappings.Any(mapping => mapping.TargetProperty == identifier.Id));
    }
}
