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

public class with_modeled_member_collisions : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _scoped = null!;
    string _source = null!;

    void Because()
    {
        var source = handler_model.Source.Replace("        handler", "        handlePlaceOrder String\n        placeOrderContext String\n        placeOrderCommand String\n        handler", StringComparison.Ordinal);
        var request = handler_model.Request(source: source);
        var slice = request.Model.Application.Modules.Single().Features.Single().Slices.Single();
        _plan = handler_model.Plan(request);
        _scoped = handler_model.Plan(request with { Scope = new(ArtifactRenderScopeKind.Slice, slice.Id) });
        _source = string.Join('\n', _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_admit_the_body_with_collisions() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_disambiguate_the_method_from_the_modeled_property() => _source.ShouldContain("HandlePlaceOrder_");
    [Fact] void should_disambiguate_the_context_from_the_modeled_property() => _source.ShouldContain("class PlaceOrderContext_");
    [Fact] void should_disambiguate_the_snapshot_from_the_modeled_property() => _source.ShouldContain("class PlaceOrderCommand_");
    [Fact] void should_keep_modeled_properties_unchanged() => _source.ShouldContain("string HandlePlaceOrder,");
    [Fact] void should_keep_the_authored_body() => _source.ShouldContain(handler_model.Body);

    [Fact]
    void should_keep_scoped_artifact_paths_and_bytes_equal()
    {
        Assert.True(_scoped.Success, string.Join('\n', _scoped.Diagnostics));
        Assert.NotEmpty(_scoped.Artifacts);
        Assert.All(_scoped.Artifacts, artifact =>
        {
            var whole = _plan.Artifacts.Single(candidate => candidate.RelativePath == artifact.RelativePath);
            whole.Bytes.AsSpan().SequenceEqual(artifact.Bytes.AsSpan()).ShouldBeTrue();
            whole.Sources.SequenceEqual(artifact.Sources).ShouldBeTrue();
        });
    }

#if DEBUG
    [Fact]
    void should_compile_the_application_without_changing_public_members() =>
        RenderedOutput.Errors(opaque_policy_model.Files(_plan)).ShouldBeEmpty();
#endif
}
