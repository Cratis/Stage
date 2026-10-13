// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_pinned_event : given.a_pinned_event
{
    SemanticFact _fact = null!;

    void Because() => _fact = SemanticWorldRebuilder.Fact(_plan, _event);

    [Fact] void should_map_the_stored_name_to_the_current_contract() => _fact.EventContract.ShouldEqual(_plan.Events.Values.Single().Id);
    [Fact] void should_keep_the_current_display_name() => _plan.Events[_fact.EventContract].Name.ShouldEqual("Renamed");
}
