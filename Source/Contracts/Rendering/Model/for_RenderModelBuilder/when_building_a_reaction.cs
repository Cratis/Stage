// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_a_reaction : given.a_response_only_command
{
    RenderReaction _reaction = null!;

    void Because()
    {
        _request = CompileRequest("""
            module Messaging
              feature Echoes
                slice Automation Reply
                  reaction EchoReaction
                    when Startup
            """);
        _slice = _request.Model.Application.Modules[0].Features[0].Slices[0];
        _reaction = RenderModelBuilder.Build(_request).Items.Reactions.Single();
    }

    [Fact] void should_keep_identity() => _reaction.Id.ShouldEqual(_slice.Reactions.Single().Id);
    [Fact] void should_classify_the_item() => _reaction.Kind.ShouldEqual("reaction");
    [Fact] void should_keep_the_original_name() => _reaction.Name.ShouldEqual("EchoReaction");
    [Fact] void should_split_name_words() => _reaction.NameWords.ShouldEqual(["Echo", "Reaction"]);
    [Fact] void should_keep_module_location() => _reaction.Location.Module.ShouldEqual(_request.Model.Application.Modules[0].Id);
    [Fact] void should_keep_outermost_first_feature_path() => _reaction.Location.FeaturePath.ShouldEqual([_request.Model.Application.Modules[0].Features[0].Id]);
    [Fact] void should_keep_slice_location() => _reaction.Location.Slice.ShouldEqual(_slice.Id);
    [Fact] void should_have_no_command_flags() => _reaction.Flags.ShouldEqual(new(false, false, false));
    [Fact] void should_mark_item_provenance_absent() => _reaction.Provenance.ShouldEqual(RenderProvenance.Absent);
    [Fact] void should_keep_trigger_payload() => _reaction.Payload.Triggers.Single().Kind.ShouldEqual(SemanticReactionTriggerKind.Startup);
    [Fact] void should_mark_trigger_provenance_absent() => _reaction.Payload.Triggers.Single().Provenance.Reason.ShouldEqual("loader does not retain compiler SourceMap");
}
