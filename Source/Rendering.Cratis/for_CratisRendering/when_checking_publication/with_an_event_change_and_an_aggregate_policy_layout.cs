// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_an_event_change_and_an_aggregate_policy_layout : given.an_event_plan
{
    void Establish()
    {
        _existing[EventPath] = "namespace Records; [EventType] public record Renamed;";
        _existing["GeneratedPolicies/Policies.cs"] = LegacyPolicies;
    }
    void Because() => _check = CratisRendering.CheckPublication(Events("namespace Records; [EventType(\"Original\")] public record Renamed;", policies: true), Read);

    [Fact] void should_prioritize_event_identity_safety() => _check.ShouldBeOfExactType<CratisPublicationCheck.ChangesEventTypeIds>();
    [Fact] void should_not_read_the_policy_layout() => _readPaths.ShouldContainOnly(EventPath);
}
#endif
