// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_invocation_expectations.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_invocation_expectations(context fixture) : IClassFixture<context>
{
    [Fact] void should_execute_generated_facts() => Assert.True(fixture.Results.Length > 30);
    [Fact] void should_pass_the_v34_golden() => AssertPassed("when_scenario");
    [Fact] void should_pass_partial_expectations() => AssertPassed("when_partial");
    [Fact] void should_preserve_same_slice_event_assertions() => AssertPassed("when_accepted");
    [Fact] void should_keep_invocations_on_the_cascade_reaction_half() => AssertPassed("when_cascade");
    [Fact] void should_fail_a_wrong_authored_value() => AssertFailed("when_wrong_value", "should_match_authored_invocation_1_reason");
    [Fact] void should_fail_a_wrong_authored_command() => AssertFailed("when_wrong_command", "should_match_authored_invocation_1_command");
    [Fact] void should_fail_a_missing_recorded_invocation() => AssertFailed("when_missing_invocation", "should_match_authored_invocation_count");
    [Fact] void should_not_fail_inferred_or_outcome_assertions() => Assert.All(fixture.Results.Where(result => result.Outcome == "Failed"), result => Assert.Contains(".should_match_authored_invocation_", result.Name, StringComparison.Ordinal));

    void AssertPassed(string name)
    {
        var results = fixture.Results.Where(result => result.Name.Contains($".{name}.", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.Equal("Passed", result.Outcome));
    }

    void AssertFailed(string name, string fact) => Assert.Contains(fixture.Results, result => result.Name.Contains($".{name}.{fact}", StringComparison.Ordinal) && result.Outcome == "Failed");

    public class context : a_generated_application
    {
        public ImmutableArray<(string Name, string Outcome)> Results { get; private set; }
        protected override bool DeferPlan => true;

        async Task Because()
        {
            try
            {
                var model = invocation_expectation_model.Create();
                var execution = SemanticExecutionPlan.Compile(model);
                Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
                var profile = CratisRendering.CreateProfile(model.Application.Name, new("BackendHost", "BackendHost"));
                var plan = _planner.Plan(new(model, execution.Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
                Assert.True(plan.Refusals.IsEmpty, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Artifact}: {diagnostic.Message}")));
                RenderApplication(plan);
                await Run("debug-build.log", "build", "BackendHost.csproj", "-c", "Debug", "--nologo", "-nodeReuse:false", "-m:2");
                await Run("debug-test.log", true, "test", "BackendHost.csproj", "-c", "Debug", "--no-build", "--nologo", "--logger", "trx;LogFileName=invocations.trx", "--results-directory", _evidence.FullName);
                XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
                var trx = XDocument.Load(Path.Combine(_evidence.FullName, "invocations.trx"));
                var methods = trx.Descendants(ns + "UnitTest")
                    .Select(test => (Id: test.Attribute("id").Value, Method: test.Element(ns + "TestMethod")))
                    .Where(test => test.Method is not null)
                    .ToDictionary(test => test.Id, test => $"{test.Method.Attribute("className").Value}.{test.Method.Attribute("name").Value}");
                Results = [.. trx.Descendants(ns + "UnitTestResult").Select(result => (methods.GetValueOrDefault(result.Attribute("testId").Value, result.Attribute("testName").Value), result.Attribute("outcome").Value))];
            }
            finally
            {
                Cleanup();
            }
        }
    }
}
#endif
