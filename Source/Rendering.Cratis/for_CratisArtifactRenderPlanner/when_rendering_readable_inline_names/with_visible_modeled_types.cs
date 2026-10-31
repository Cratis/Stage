// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
#endif
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_readable_inline_names;

public class with_visible_modeled_types : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _source = null!;

    void Because()
    {
        const string source = "concept PlaceOrderContext : String\nconcept PlaceOrderCommand : String\n" + handler_model.Source;
        _plan = handler_model.Plan(handler_model.Request(source: source));
        _source = string.Join('\n', _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_admit_the_body() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_not_shadow_the_modeled_context_type() => _source.ShouldContain("class PlaceOrderContext_");
    [Fact] void should_not_shadow_the_modeled_snapshot_type() => _source.ShouldContain("class PlaceOrderCommand_");
    [Fact] void should_not_suffix_the_independent_method() => _source.ShouldContain("HandlePlaceOrder(");
    [Fact] void should_keep_the_modeled_type_name() => _source.ShouldContain("record PlaceOrderContext(");
#if DEBUG
    [Fact] void should_compile_with_the_modeled_types() => RenderedOutput.Errors(opaque_policy_model.Files(_plan)).ShouldBeEmpty();
#endif
}
