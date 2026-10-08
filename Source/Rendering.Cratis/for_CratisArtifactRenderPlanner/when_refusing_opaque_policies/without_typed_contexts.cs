// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_opaque_policies;

/// <summary>
/// A body is never compiled against an untyped or reconstructed context.
/// </summary>
public class without_typed_contexts : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source("return true;"));
        _plan = opaque_policy_model.Plan(loaded with { TypedContextDescriptors = [] });
    }

    [Fact] void should_render_nothing() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_missing_policy_context() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("PolicyContext", StringComparison.Ordinal));
}
#endif
