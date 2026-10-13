// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_duplicate_type_names_in_different_namespaces : given.an_event_plan
{
    void Establish() => _existing[EventPath] = """
        namespace Records . @Recording { [EventType("Original")] public record Renamed; }
        namespace Other { [EventType("Other")] public record Renamed; }
        """;
    void Because() => _check = CratisRendering.CheckPublication(Events("namespace Records.Recording { [EventType(\"Original\")] public record Renamed; }"), Read);

    [Fact] void should_use_the_canonical_qualified_name_as_a_tiebreak() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
}
#endif
