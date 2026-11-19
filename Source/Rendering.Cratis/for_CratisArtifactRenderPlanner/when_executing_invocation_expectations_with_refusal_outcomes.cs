// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_invocation_expectations_with_refusal_outcomes.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_invocation_expectations_with_refusal_outcomes(context fixture) : IClassFixture<context>
{
    [Fact] void should_execute_generated_facts() => Assert.True(fixture.Results.Length > 6);
    [Fact] void should_pass_every_fact_when_the_refusal_is_acknowledged_and_nothing_is_appended() => AssertOutcome("when_silent", "Passed");
    [Fact] void should_keep_the_invocation_assertions() => Assert.Contains(fixture.Results, result => result.Matches("when_silent", "should_match_authored_invocation_1_id"));
    [Fact] void should_fail_when_the_command_succeeds_and_appends() => AssertFailed("when_succeeding", "should_append_exactly_the_expected_events");
    [Fact] void should_fail_when_the_invocation_value_is_wrong() => AssertFailed("when_mismatching", "should_match_authored_invocation_1_id");

    void AssertOutcome(string name, string outcome)
    {
        var results = fixture.Results.Where(result => result.Matches(className: name)).ToArray();
        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.Equal(outcome, result.Outcome));
    }

    void AssertFailed(string name, string fact) =>
        Assert.Contains(fixture.Results, result => result.Matches(name, fact) && result.Outcome == "Failed");

    public class context : a_generated_application
    {
        public ImmutableArray<GeneratedTestResult> Results { get; private set; }
        protected override bool DeferPlan => true;

        async Task Because()
        {
            try
            {
                var model = invocation_refusal_outcome_model.Create();
                var execution = SemanticExecutionPlan.Compile(model);
                Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
                var profile = CratisRendering.CreateProfile(model.Application.Name, new("BackendHost", "BackendHost"));
                var plan = _planner.Plan(new(model, execution.Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
                Assert.True(plan.Refusals.IsEmpty, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Artifact}: {diagnostic.Message}")));
                RenderApplication(plan);
                await Run("debug-build.log", "build", "BackendHost.csproj", "-c", "Debug", "--nologo", "-nodeReuse:false", "-m:2");
                await Run("debug-test.log", true, "test", "BackendHost.csproj", "-c", "Debug", "--no-build", "--nologo", "--logger", "trx;LogFileName=refusal-outcomes.trx", "--results-directory", _evidence.FullName);
                Results = GeneratedTestResults.Read(XDocument.Load(Path.Combine(_evidence.FullName, "refusal-outcomes.trx")));
            }
            finally
            {
                Cleanup();
            }
        }
    }
}
#endif
