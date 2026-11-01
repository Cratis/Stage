// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_selected_scopes : given.a_response_only_command
{
    SemanticModule _module = null!;
    SemanticFeature _outer = null!;
    SemanticFeature _inner = null!;
    SemanticSlice _nested = null!;
    SemanticCommand _nestedCommand = null!;
    SemanticReaction _nestedReaction = null!;

    void Establish()
    {
        _request = CompileRequest("""
            module Messaging
              feature Echoes
                slice StateChange Reply
                  command Echo
                    message String
                    returns message
                feature Nested
                  slice StateChange ReplyAgain
                    command NestedEcho
                      message String
                      returns message
                  slice Automation React
                    reaction NestedReaction
                      when Startup
            module Other
              feature Elsewhere
                slice StateChange OtherReply
                  command OtherEcho
                    message String
                    returns message
                slice Automation OtherReact
                  reaction OtherReaction
                    when Shutdown
            """);
        _module = _request.Model.Application.Modules[0];
        _outer = _module.Features[0];
        _command = _outer.Slices[0].Commands[0];
        _inner = _outer.Features[0];
        _nested = _inner.Slices[0];
        _nestedCommand = _nested.Commands[0];
        _nestedReaction = _inner.Slices[1].Reactions[0];
    }

    [Fact]
    void should_select_only_the_requested_slice()
    {
        var result = RenderModelBuilder.Build(_request with { Scope = new(ArtifactRenderScopeKind.Slice, _nested.Id) });
        result.Items.Commands.Select(value => value.Id).ShouldEqual([_nestedCommand.Id]);
        result.Items.Reactions.ShouldBeEmpty();
        result.Scope.Slices.Select(value => value.Id).ShouldEqual([_nested.Id]);
        result.Items.Commands.Single().Location.FeaturePath.ShouldEqual([_outer.Id, _inner.Id]);
    }

    [Fact]
    void should_select_a_module_and_its_nested_features()
    {
        var result = RenderModelBuilder.Build(_request with { Scope = new(ArtifactRenderScopeKind.Module, _module.Id) });
        result.Items.Commands.Select(value => value.Id).ShouldContainOnly(_command.Id, _nestedCommand.Id);
        result.Items.Reactions.Select(value => value.Id).ShouldEqual([_nestedReaction.Id]);
        result.Scope.Slices.Count.ShouldEqual(3);
    }

    [Fact]
    void should_select_an_outer_feature_and_its_descendants()
    {
        var result = RenderModelBuilder.Build(_request with { Scope = new(ArtifactRenderScopeKind.Feature, _outer.Id) });
        result.Items.Commands.Select(value => value.Id).ShouldContainOnly(_command.Id, _nestedCommand.Id);
        result.Items.Reactions.Select(value => value.Id).ShouldEqual([_nestedReaction.Id]);
        result.Scope.Slices.Count.ShouldEqual(3);
    }

    [Fact]
    void should_exclude_the_parent_when_selecting_a_nested_feature()
    {
        var result = RenderModelBuilder.Build(_request with { Scope = new(ArtifactRenderScopeKind.Feature, _inner.Id) });
        result.Items.Commands.Select(value => value.Id).ShouldEqual([_nestedCommand.Id]);
        result.Items.Reactions.Select(value => value.Id).ShouldEqual([_nestedReaction.Id]);
        result.Scope.Slices.Select(value => value.Id).ShouldContainOnly([.. _inner.Slices.Select(value => value.Id)]);
    }
}
