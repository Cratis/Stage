// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Commands;
using Cratis.Concepts;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs.given;

internal static class clr_date_inputs
{
    public record UtcMoment(DateTime Value) : ConceptAs<DateTime>(Value);
    public record OffsetMoment(DateTimeOffset Value) : ConceptAs<DateTimeOffset>(Value);
    public record Command(
        [property: ExplicitOffset] UtcMoment At,
        [property: ExplicitOffset] OffsetMoment OffsetAt,
        [property: ExplicitOffset] UtcMoment? OptionalAt = null,
        [property: ExplicitOffset] OffsetMoment? OptionalOffsetAt = null);
}
#endif
