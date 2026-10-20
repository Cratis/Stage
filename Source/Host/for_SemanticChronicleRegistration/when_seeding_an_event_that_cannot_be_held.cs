// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_seeding_an_event_that_cannot_be_held : a_seeding_store
{
    void Establish() => Prepare("""
        seed
          for "half"
            ProductListed
              name = "A product without a price"
          for "tee"
            ProductListed
              name = "Organic cotton tee"
              price = 24
              featured = false
              tags = []
        """);

    async Task Because() => await SemanticChronicleRegistration.Register(_client, Store, _plan, _seeds);

    [Fact] void should_leave_the_incomplete_event_out_rather_than_poison_the_log() => _appends.SelectMany(append => append.Events).Select(@event => @event.EventSourceId).ShouldContainOnly(["tee"]);
    [Fact] void should_say_why() => _seeds.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(SemanticSeedPlan.NotReplayable);
}
