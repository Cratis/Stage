// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static bool HasOneOutcome(SemanticSpecification specification)
    {
        var rejects = !specification.ThenErrors.IsEmpty;
        var succeeds = !specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty;
        return rejects != succeeds;
    }

    static bool HasSupportedCounts(SemanticSpecification specification) =>
        specification.ThenEvents.Length <= 1 && specification.ThenReadModels.Length <= 1 &&
        specification.ThenQueries.Length <= 1 && specification.ThenErrors.Length <= 1;
}
