// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_locating_errors_in_re_indented_bodies;

internal static class located_error
{
    // The authored second line is `return missing...`; `missing` starts at column 8 of that line.
    internal static void ShouldPointAtTheAuthoredSecondLine(LoadedSemanticModel loaded, ArtifactRenderPlan plan, string prefix)
    {
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var expected = $"{requirement.Source.Span.Document}:{requirement.BodyLines[1].Line}:{requirement.BodyLines[1].Column + 7}";
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains(prefix, StringComparison.Ordinal) &&
            diagnostic.Message.Contains(expected, StringComparison.Ordinal) && diagnostic.Message.Contains("CS0103", StringComparison.Ordinal));
    }
}
#endif
