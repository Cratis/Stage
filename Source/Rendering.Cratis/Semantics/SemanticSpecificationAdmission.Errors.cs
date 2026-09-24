// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks specification outcomes and rejection counts.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    static bool HasOneOutcome(SemanticSpecification specification)
    {
        var rejects = !specification.ThenErrors.IsEmpty || specification.ThenDenied;
        if (specification.ThenDenied && !specification.ThenErrors.IsEmpty)
        {
            return false;
        }
        var succeeds = !specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty;
        return rejects != succeeds;
    }

    static bool HasSupportedCounts(SemanticSpecification specification) =>
        specification.ThenErrors.Length <= 1 &&
        specification.ThenReadModels.Select(_ => _.ReadModel).Distinct().Count() == specification.ThenReadModels.Length &&
        specification.ThenQueries.Select(_ => _.Query).Distinct().Count() == specification.ThenQueries.Length;
}
