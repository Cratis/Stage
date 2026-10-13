// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_filtered_productions : given.a_filtered_reaction
{
    void Because() => Render();
    [Fact] void should_assert_the_default_source() => _source.ShouldContain("EventSourceType == global::Cratis.Chronicle.Events.EventSourceType.Default");
    [Fact] void should_assert_the_default_stream_identity() => _source.ShouldContain("EventStreamId == global::Cratis.Chronicle.Events.EventStreamId.Default");
    [Fact] void should_preserve_the_append_route() => _source.ShouldContain("OnStream(eventSourceType: \"stored-account\", eventStreamType: \"stored-notes\"");
    [Fact] void should_explain_why_history_is_not_delivered() => _source.ShouldContain("Given facts establish history; reactions only observe the append.");
}
