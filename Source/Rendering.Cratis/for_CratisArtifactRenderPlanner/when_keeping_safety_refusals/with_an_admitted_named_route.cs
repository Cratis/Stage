// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_keeping_safety_refusals;

public class with_an_admitted_named_route : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = invoice_model.Compile(when_planning_event_source_routes.Source);

    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_return_a_complete_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_not_refuse_the_named_route() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-030" || diagnostic.Code == "STAGE-ESM-036").ShouldBeEmpty();
    [Fact] void should_emit_the_event_source_definition() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath == "EventSources/AccountEventSource.cs");
    [Fact] void should_retain_the_named_stream_in_the_command() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Deposit.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("Transactions");
    [Fact] void should_not_omit_any_artifacts() => _plan.Refusals.ShouldBeEmpty();
}
#endif
