// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_an_event_type_id_change : given.an_event_plan
{
    CratisPublicationCheck _applicationCheck = null!;

    void Establish() => _existing[EventPath] = "namespace Records; [EventType] public record Renamed;";
    void Because()
    {
        const string code = "namespace Records; [global::Cratis.Chronicle.Events.EventTypeAttribute(\"Original\")] public record Renamed;";
        _check = CratisRendering.CheckPublication(Events(code), Read);
        _applicationCheck = CratisRendering.CheckPublication(Events(code, application: true), Read);
    }

    [Fact] void should_refuse_scoped_publication() => _check.ShouldBeOfExactType<CratisPublicationCheck.ChangesEventTypeIds>();
    [Fact] void should_refuse_application_publication() => _applicationCheck.ShouldBeOfExactType<CratisPublicationCheck.ChangesEventTypeIds>();
    [Fact] void should_identify_the_changed_type() => ((CratisPublicationCheck.ChangesEventTypeIds)_check).Changes.ShouldContainOnly(new CratisEventTypeIdChange(EventPath, "Records.Renamed", "Renamed", "Original"));
    [Fact] void should_report_the_event_identity_diagnostic() => ((CratisPublicationCheck.ChangesEventTypeIds)_check).Diagnostics.Single().Code.ShouldEqual("STAGE-EVENT-002");
    [Fact] void should_report_an_error() => ((CratisPublicationCheck.ChangesEventTypeIds)_check).Diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_explain_the_pin_remedy() => ((CratisPublicationCheck.ChangesEventTypeIds)_check).Diagnostics.Single().Message.ShouldContain("id \"Renamed\"");
}
#endif
