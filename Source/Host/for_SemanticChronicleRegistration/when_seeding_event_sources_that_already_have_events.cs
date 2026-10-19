// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_seeding_event_sources_that_already_have_events : a_seeding_store
{
    void Establish()
    {
        Prepare("""
            seed
              for "mug"
                ProductListed
                  name = "Stoneware mug"
                  price = 12.5
                  featured = true
                  tags = []
              for "tee"
                ProductListed
                  name = "Organic cotton tee"
                  price = 24
                  featured = false
                  tags = []
            """);
        _sourcesWithEvents.Add("mug");
    }

    async Task Because()
    {
        // The tail is read as empty so the world is the seeded one; the point is which sources are appended to.
        await SemanticChronicleRegistration.Register(_client, Store, _plan, _seeds);
    }

    [Fact] void should_leave_the_source_that_has_events_alone() => _appends.SelectMany(append => append.Events).Select(@event => @event.EventSourceId).ShouldContainOnly(["tee"]);
}
