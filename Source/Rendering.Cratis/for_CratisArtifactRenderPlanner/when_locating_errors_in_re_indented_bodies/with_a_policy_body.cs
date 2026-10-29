// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_locating_errors_in_re_indented_bodies;

public class with_a_policy_body : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _loaded = opaque_policy_model.Load(opaque_policy_model.Source("_ = 1;\nreturn missing;"));
        _plan = opaque_policy_model.Plan(_loaded);
    }

    [Fact] void should_locate_the_error_in_the_authored_body() => located_error.ShouldPointAtTheAuthoredSecondLine(_loaded, _plan, "Policy body");
}
#endif
