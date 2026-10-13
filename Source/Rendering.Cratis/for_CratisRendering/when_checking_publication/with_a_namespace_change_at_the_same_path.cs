// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_a_namespace_change_at_the_same_path : given.an_event_plan
{
    void Establish() => _existing[EventPath] = "namespace OldRecords; [EventType] public record Renamed;";
    void Because() => _check = CratisRendering.CheckPublication(Events("namespace Records; [EventType(\"Original\")] public record Renamed;"), Read);

    [Fact] void should_refuse_the_stored_id_change() => _check.ShouldBeOfExactType<CratisPublicationCheck.ChangesEventTypeIds>();
    [Fact] void should_report_the_planned_qualified_name() => ((CratisPublicationCheck.ChangesEventTypeIds)_check).Changes.ShouldContainOnly(new CratisEventTypeIdChange(EventPath, "Records.Renamed", "Renamed", "Original"));
}
#endif
