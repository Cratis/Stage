// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_locating_errors_in_re_indented_bodies;

public class with_a_reducer_body : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;

    async Task Because()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace("return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);", "_ = 1;\n          return missing;", StringComparison.Ordinal);
        _loaded = await when_rendering_a_pure_reducer.Load(source);
        _plan = when_rendering_a_pure_reducer.Plan(_loaded);
    }

    [Fact] void should_locate_the_error_in_the_authored_body() => located_error.ShouldPointAtTheAuthoredSecondLine(_loaded, _plan, "Reducer body");
}
#endif
