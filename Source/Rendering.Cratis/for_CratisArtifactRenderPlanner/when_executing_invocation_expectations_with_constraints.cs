// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_invocation_expectations_with_constraints.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_invocation_expectations_with_constraints(context fixture) : IClassFixture<context>
{
    [Fact] void should_match_the_invocation_for_an_available_value() => AssertInvocation("when_recorded", "Passed");
    [Fact] void should_match_the_invocation_for_an_already_claimed_value() => AssertInvocation("when_claimed", "Passed");
    [Fact] void should_reject_a_mismatched_invocation() => AssertInvocation("when_mismatched", "Failed");
    [Fact] void should_pass_every_positive_generated_fact() => Assert.All(fixture.Results.Where(result => !result.Matches(className: "when_mismatched")), result => Assert.Equal("Passed", result.Outcome));

    void AssertInvocation(string className, string outcome)
    {
        var result = Assert.Single(fixture.Results.Where(result => result.Matches(className: className, methodName: "should_match_authored_invocation_1_id")));
        Assert.Equal(outcome, result.Outcome);
    }

    public class context : a_generated_application
    {
        public ImmutableArray<GeneratedTestResult> Results { get; private set; }
        protected override bool DeferPlan => true;

        async Task Because()
        {
            try
            {
                var source = invocation_constraint_model.Source.Replace(
                    "    slice StateChange Completion",
                    invocation_constraint_model.Claimed + "\n" + invocation_constraint_model.Mismatched + "\n    slice StateChange Completion",
                    StringComparison.Ordinal);
                var model = Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);
                var execution = SemanticExecutionPlan.Compile(model);
                Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
                var profile = CratisRendering.CreateProfile(model.Application.Name, new("BackendHost", "BackendHost"));
                var plan = _planner.Plan(new(model, execution.Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
                Assert.True(plan.Refusals.IsEmpty, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Artifact}: {diagnostic.Message}")));
                RenderApplication(plan);
                await Run("debug-build.log", "build", "BackendHost.csproj", "-c", "Debug", "--nologo", "-warnaserror", "-nodeReuse:false", "-m:2");
                await Run("debug-test.log", true, "test", "BackendHost.csproj", "-c", "Debug", "--no-build", "--nologo", "--logger", "trx;LogFileName=constraint-invocations.trx", "--results-directory", _evidence.FullName);
                Results = GeneratedTestResults.Read(XDocument.Load(Path.Combine(_evidence.FullName, "constraint-invocations.trx")));
            }
            finally
            {
                Cleanup();
            }
        }
    }
}
#endif
