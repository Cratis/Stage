// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_composite_and_colliding_responses(when_comparing_composite_and_colliding_responses.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_composite_and_colliding_responses.context>
{
    [Fact] void should_preserve_composite_and_escaped_responses() => Assert.Equal(3, fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)));
    [Fact] void should_fail_an_incorrect_composite_response_on_every_path() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongComposite: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_render_the_escaped_response_record() => Assert.Contains("record RelocateResponse_(", fixture.Slice, StringComparison.Ordinal);
    [Fact] void should_keep_the_colliding_command_name() => Assert.Contains("record RelocateResponse(", fixture.Slice, StringComparison.Ordinal);
    [Fact] void should_report_the_escaped_response_record() => Assert.Contains(fixture.Plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-050" && diagnostic.Message.Contains("'RelocateResponse_'", StringComparison.Ordinal));

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Invoices";
        protected override string ProjectFile => "InvoiceApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("generated", invoice_model.Compile(Source))];
        public ArtifactRenderPlan Plan { get; private set; } = null!;
        public string Slice => Encoding.UTF8.GetString(Plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Move/Move.cs", StringComparison.Ordinal)).Bytes.AsSpan());

        Task Because()
        {
            Plan = CreatePlan();
            return Verify();
        }

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
                  command Relocate
                    projectId New generated identifier
                    address Address
                    produces event Relocated
                      address Address = address
                    returns
                      projectId = projectId
                      address = address
                  command RelocateResponse
                    projectId New generated identifier
                    address Address
                    produces event RelocationRecorded
                      address Address = address
                  specification CompositeResponse
                    when Move
                      for "11111111-1111-1111-1111-111111111111"
                      address = {"street":"Main","city":"Oslo"}
                    then Moved
                      address = {"street":"Main","city":"Oslo"}
                    then returns {"street":"Main","city":"Oslo"}
                  specification EscapedResponse
                    when Relocate
                      for "11111111-1111-1111-1111-111111111111"
                      address = {"street":"Main","city":"Oslo"}
                    then Relocated
                      address = {"street":"Main","city":"Oslo"}
                    then returns
                      projectId = "11111111-1111-1111-1111-111111111111"
                      address = {"street":"Main","city":"Oslo"}
                  specification GeneratedNew
                    when RelocateResponse
                      for "22222222-2222-2222-2222-222222222222"
                      address = {"street":"Side","city":"Bergen"}
                    then RelocationRecorded
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
