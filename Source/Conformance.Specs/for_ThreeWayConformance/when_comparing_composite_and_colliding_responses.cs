// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_composite_and_colliding_responses(when_comparing_composite_and_colliding_responses.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_composite_and_colliding_responses.context>
{
    [Fact] void should_preserve_composite_and_escaped_responses() => Assert.Equal(3, fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)));
    [Fact] void should_fail_an_incorrect_composite_response_on_every_path() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongComposite: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Invoices";
        protected override string ProjectFile => "InvoiceApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("generated", invoice_model.Compile(Source))];
        Task Because() => Verify();

        const string Source = """
            concept New : Uuid
            type Address
              street String
              city String
            module Projects
              feature Moving
                slice StateChange Move
                  command Move
                    projectId New generated identifier
                    address Address
                    produces event Moved
                      address Address = address
                    returns address
                  command MoveResponse
                    projectId New generated identifier
                    address Address
                    produces event MoveRecorded
                      address Address = address
                    returns
                      projectId = projectId
                      address = address
                  specification CompositeResponse
                    when Move
                      for "11111111-1111-1111-1111-111111111111"
                      address = {"street":"Main","city":"Oslo"}
                    then Moved
                      address = {"street":"Main","city":"Oslo"}
                    then returns {"street":"Main","city":"Oslo"}
                  specification EscapedResponse
                    when MoveResponse
                      for "11111111-1111-1111-1111-111111111111"
                      address = {"street":"Main","city":"Oslo"}
                    then MoveRecorded
                      address = {"street":"Main","city":"Oslo"}
                    then returns
                      projectId = "11111111-1111-1111-1111-111111111111"
                      address = {"street":"Main","city":"Oslo"}
                  specification GeneratedNew
                    when MoveResponse
                      for "22222222-2222-2222-2222-222222222222"
                      address = {"street":"Side","city":"Bergen"}
                    then MoveRecorded
                      address = {"street":"Side","city":"Bergen"}
                  specification WrongComposite
                    when Move
                      for "11111111-1111-1111-1111-111111111111"
                      address = {"street":"Main","city":"Oslo"}
                    then Moved
                      address = {"street":"Main","city":"Oslo"}
                    then returns {"street":"Main","city":"Bergen"}
            """;
    }
}
#endif
