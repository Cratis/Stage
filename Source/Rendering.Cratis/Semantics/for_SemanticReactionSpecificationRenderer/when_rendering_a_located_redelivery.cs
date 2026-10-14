// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_a_located_redelivery : given.a_redelivery_specification
{
    void Because() => Render();

    [Fact] void should_select_only_the_named_reactor() => _source.ShouldContain("ReactorScenario<global::Reactions.Work.Processing.FollowUp.Worker>");
    [Fact] void should_record_the_first_given_without_observing_it() => _source.ShouldContain("var occurrence0 = await _reactor.Given.ForEventSource(\"earlier\").History(");
    [Fact] void should_record_the_selected_given_with_its_source() => _source.ShouldContain("var occurrence1 = await _reactor.Given.ForEventSource(\"source\").History(");
    [Fact] void should_deliver_the_existing_nonzero_occurrence() => _source.ShouldContain("await _reactor.Redeliver(occurrence1);");
    [Fact] void should_not_allocate_a_fresh_delivery() => _source.ShouldNotContain(".Events(");
    [Fact] void should_assert_only_the_new_effects() => _source.ShouldContain("_reactor.ProducedEvents.Count.ShouldEqual(1)");
}
