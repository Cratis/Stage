// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_generated_values_and_responses(when_comparing_generated_values_and_responses.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_generated_values_and_responses.context>
{
    [Fact] void should_preserve_required_and_optional_scalar_and_record_responses() => Assert.Equal(6, fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)));
    [Fact] void should_fail_every_wrong_optional_response() => Assert.Equal(4, fixture.Outcomes.Count(outcome => outcome.Contains("/WrongOptional", StringComparison.Ordinal) && outcome.Contains("Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal)));
    [Fact] void should_fail_an_incorrect_response_on_every_path() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongResponse: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Invoices";
        protected override string ProjectFile => "InvoiceApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("generated", optional_scalar_response_cases.Compile(Source))];
        Task Because() => Verify();

        const string Source = """
            concept ProjectId : Uuid
            concept Receipt : Uuid
            module Projects
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId ProjectId generated identifier
                    name String
                    produces event ProjectRegistered
                      name String = name
                    returns projectId
                  specification ScalarResponse
                    when RegisterProject
                      for "11111111-1111-1111-1111-111111111111"
                      name = "Apollo"
                    then ProjectRegistered
                      name = "Apollo"
                    then returns "11111111-1111-1111-1111-111111111111"
                slice StateChange RegisterReceipt
                  command RegisterReceipt
                    projectId ProjectId generated identifier
                    receipt Receipt generated
                    name String
                    produces event ReceiptRegistered
                      receipt Receipt = receipt
                      name String = name
                    returns
                      projectId = projectId
                      receipt = receipt
                      name = name
                  specification RecordResponse
                    when RegisterReceipt
                      for "11111111-1111-1111-1111-111111111111"
                      generated receipt = "22222222-2222-2222-2222-222222222222"
                      name = "Apollo"
                    then ReceiptRegistered
                      receipt = "22222222-2222-2222-2222-222222222222"
                      name = "Apollo"
                    then returns
                      projectId = "11111111-1111-1111-1111-111111111111"
                      receipt = "22222222-2222-2222-2222-222222222222"
                      name = "Apollo"
                  specification WrongResponse
                    when RegisterReceipt
                      for "11111111-1111-1111-1111-111111111111"
                      generated receipt = "22222222-2222-2222-2222-222222222222"
                      name = "Apollo"
                    then ReceiptRegistered
                      receipt = "22222222-2222-2222-2222-222222222222"
                      name = "Apollo"
                    then returns
                      name = "Different"
            """;
    }
}
#endif
