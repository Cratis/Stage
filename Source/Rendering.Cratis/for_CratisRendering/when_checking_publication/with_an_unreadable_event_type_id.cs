// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_an_unreadable_event_type_id : given.an_event_plan
{
    CratisPublicationCheck.ChangesEventTypeIds[] _checks = [];

    void Because()
    {
        var plan = Events("namespace Records; [EventType(\"Original\")] public record Renamed;");
        _checks = [.. new[] { "namespace Records; [EventType(StoredId)] public record Renamed;", "namespace Records; [EventType($\"{StoredId}\")] public record Renamed;", "namespace Records; [EventType(\"Original\")] public record Renamed(" }
            .Select(code => (CratisPublicationCheck.ChangesEventTypeIds)CratisRendering.CheckPublication(plan, _ => code))];
    }

    [Fact] void should_refuse_every_undeterminable_identity() => _checks.Length.ShouldEqual(3);
    [Fact] void should_retain_the_planned_identity() => _checks.All(check => check.Changes.Single().PlannedId == "Original").ShouldBeTrue();
    [Fact] void should_explain_that_the_existing_id_cannot_be_read() => _checks.All(check => check.Diagnostics.Single().Message.Contains("existing [EventType] id that cannot be read", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
