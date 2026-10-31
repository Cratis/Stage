// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers;

public class a_live_optional_performer : Specification
{
    ArtifactRenderPlan _plan = null!;

    async Task Because()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(given.a_performer.Source("return readModels;", cardinality: " optional"));
        _plan = when_rendering_a_pure_reducer.Plan(loaded);
    }

    [Fact] void should_keep_the_existing_refusal() => _plan.Success.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Message.Contains("only a collection can be observed.", StringComparison.Ordinal));
}
