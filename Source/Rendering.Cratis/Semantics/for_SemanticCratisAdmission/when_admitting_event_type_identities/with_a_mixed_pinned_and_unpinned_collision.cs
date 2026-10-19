// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_event_type_identities;

public class with_a_mixed_pinned_and_unpinned_collision : given.a_mixed_event_model
{
    void Because() => _plan = Plan("RecordedEvent");

    [Fact] void should_refuse_the_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_not_emit_any_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_generated_identity_collision() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity != global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Information).Single().Code.ShouldEqual("STAGE-ESM-012");
    [Fact] void should_name_both_events_and_the_chronicle_identity() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity != global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Information).Single().Message.ShouldEqual("Events 'Recorded_event', 'Renamed' collide on generated Chronicle event type id 'RecordedEvent' at generation 1; stored names and unpinned generated C# names must be unique within a generation.");
}
