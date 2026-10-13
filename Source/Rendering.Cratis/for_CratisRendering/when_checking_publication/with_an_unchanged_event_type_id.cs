// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_an_unchanged_event_type_id : given.an_event_plan
{
    CratisPublicationCheck[] _checks = [];

    void Because()
    {
        var plan = Events("namespace Records; [EventType] public record Renamed;");
        _checks = new[] { "[EventType]", "[EventType(\"Renamed\")]", "[EventType(\"\")]", "[EventType(id: \"Renamed\", generation: 1)]", "[Cratis.Chronicle.Events.EventTypeAttribute(\"Renamed\")]" }
            .Select(attribute => CratisRendering.CheckPublication(plan, _ => $"namespace Records; {attribute} public record Renamed;")).ToArray();
    }

    [Fact] void should_preserve_equivalent_stored_identities() => _checks.All(check => check is CratisPublicationCheck.Compatible).ShouldBeTrue();
}
#endif
