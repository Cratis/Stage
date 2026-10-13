// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_a_pinned_rename : given.an_event_plan
{
    void Establish() => _existing[EventPath] = "namespace Records; [EventType] public record Original;";
    void Because() => _check = CratisRendering.CheckPublication(Events("namespace Records; [EventType(\"Original\")] public record Renamed;"), Read);

    [Fact] void should_preserve_the_pinned_identity() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
}
#endif
