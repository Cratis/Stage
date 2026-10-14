// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_refusing_unsettled_no_event_specifications : Specification
{
    [Theory]
    [InlineData("no-observer", "no-observer")]
    [InlineData("fan-out", "fan-out")]
    [InlineData("cascade", "deeper reaction cascades")]
    [InlineData("command", "do not settle reactions")]
    [InlineData("invocation", "production-only")]
    public void should_refuse_without_partial_artifacts(string form, string reason)
    {
        var observed = form == "no-observer" ? "Finished" : "Started";
        var effect = form == "invocation" ? "invokes Complete\n            id = id" : "produces Followed\n            for id\n            id = id";
        var extra = form switch
        {
            "fan-out" => """
                  reaction Sibling
                    when Started
                      produces Followed
                        for id
                        id = id
            """,
            "cascade" => """
                  reaction Finisher
                    when Followed
                      produces Finished
                        for id
                        id = id
            """,
            _ => ""
        };
        var action = form == "command" ? "when Begin\n          id = \"one\"" : "when append Started\n          for \"one\"\n          id = \"one\"";
        var model = a_recovery_plan.Compile($"""
            module Work
              feature Items
                slice Automation Recovery
                  command Begin
                    id String identifier
                    produces Started
                      for id
                      id = id
                  command Complete
                    id String identifier
                    produces Followed
                      for id
                      id = id
                  event Started
                    id String
                  event Followed
                    id String
                  event Finished
                    id String
                  reaction Worker
                    when {observed}
                      {effect}
            {extra}
                  specification NothingNew
                    {action}
                    then no events
            """);
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Recovery", "Recovery"));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldBeTrue();
    }
}
#endif
