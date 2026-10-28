// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
#endif
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_readable_inline_names;

public class with_a_command_handler : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _source = null!;

    void Because()
    {
        _plan = handler_model.Plan(handler_model.Request());
        _source = string.Join('\n', _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_admit_the_same_verified_body() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_name_the_private_body_after_its_command() => _source.ShouldContain("HandlePlaceOrder(PlaceOrderContext context)");
    [Fact] void should_name_the_snapshot_after_its_command() => _source.ShouldContain("class PlaceOrderCommand(");
    [Fact] void should_keep_the_authored_body() => _source.ShouldContain(handler_model.Body);
    [Fact] void should_remove_the_default_hashed_body_name() => Assert.DoesNotContain("HandlerBody_", _source, StringComparison.Ordinal);

#if DEBUG
    [Fact]
    void should_execute_the_authored_handler_through_the_public_handle()
    {
        var assembly = opaque_policy_model.Compile(_plan);
        var type = assembly.GetTypes().Single(type => type.Name == "PlaceOrder");
        var command = Activator.CreateInstance(type, Guid.Empty, "sample");
        var result = type.GetMethod("Handle").Invoke(command, []);
        result.GetType().GetProperty("Name").GetValue(result).ShouldEqual("SAMPLE");
    }
#endif
}
