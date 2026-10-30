// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_readable_inline_names;

public class with_a_handler_method_matching_a_modeled_type : Specification
{
    const string Body = "return new OrderPlaced(HandlePlaceOrder.Accepted.ToString());";
    ArtifactRenderPlan _plan = null!;
    string _source = null!;

    void Because()
    {
        var source = "concept HandlePlaceOrder : Enum\n  accepted\n" + handler_model.Source.Replace("orderId Uuid identifier", "orderId Uuid identifier\n        status HandlePlaceOrder", StringComparison.Ordinal);
        _plan = handler_model.Plan(handler_model.Request(body: Body, source: source));
        _source = string.Join('\n', _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_admit_the_authored_body() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_suffix_the_method() => _source.ShouldContain("HandlePlaceOrder_");
    [Fact] void should_preserve_the_authored_body() => _source.ShouldContain(Body);
    [Fact]
    void should_compile_the_generated_application()
    {
        var errors = RenderedOutput.Errors(opaque_policy_model.Files(_plan));
        Assert.True(errors.Count == 0, string.Join('\n', errors));
    }
}
#endif
