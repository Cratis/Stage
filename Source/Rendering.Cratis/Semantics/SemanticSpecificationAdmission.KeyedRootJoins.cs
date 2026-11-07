// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static IEnumerable<SemanticSpecification> WithoutDivergentRootJoins(
        IEnumerable<SemanticSpecification> specifications,
        SemanticApplicationContext context,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in specifications)
        {
            if (SemanticKeyedRootJoinOrdering.Divergence(context.Projections.Values, specification) is { } reason)
            {
                diagnostics.Add(RefusalDiagnostic(context, specification, reason));
                continue;
            }
            yield return specification;
        }
    }

    static bool HasRenderableOrderedGivenEvents(SemanticApplicationContext context, SemanticSpecification specification) =>
        SemanticKeyedRootJoinOrdering.Divergence(context.Projections.Values, specification) is null &&
        HasRenderableGivenEvents(context, specification);
}
