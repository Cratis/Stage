// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_a_model_with_duplicate_identities : given.a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;
    Exception? _error;
    SemanticId _duplicateId;

    void Establish()
    {
        // Fault injection: duplicate a compiled event without changing the valid source fixture.
        var @event = _registerProject.Events.Single();
        _duplicateId = @event.Id;
        typeof(SemanticSlice).GetField("<Events>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_registerProject, ImmutableArray.Create(@event, @event));
    }

    void Because() => _error = Catch.Exception(() => _plan = _planner.Plan(_request));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_report_the_duplicate_event_identity() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Message.Contains($"duplicate event identity '{_duplicateId}'", StringComparison.Ordinal));
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
