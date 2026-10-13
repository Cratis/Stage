// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_generated_value_destination_collisions(when_comparing_generated_value_destination_collisions.context fixture) : IClassFixture<when_comparing_generated_value_destination_collisions.context>
{
    [Fact] void should_agree_on_colliding_and_distinct_destinations() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_have_no_three_way_differences() => Assert.Empty(fixture.Differences);

    public class context : a_three_way_application
    {
        const string Source = """
            concept Receipt : String
              generate nanoid
            concept GeneratedId : Uuid
              generate uuidv7
            module Billing
              feature Receipts
                slice StateChange TextReceipt
                  event Recorded
                  command Record
                    id Receipt identifier
                    receipt Receipt generated
                    produces Recorded
                      for id
                  specification MatchingReceipt
                    given Recorded
                      for "ABCDEFGHIJKLMNOPQRSTU"
                    when Record
                      id = "ABCDEFGHIJKLMNOPQRSTU"
                      generated receipt = "ABCDEFGHIJKLMNOPQRSTU"
                    then error "A generated event source already has facts; retry the command with a fresh generated value."
                  specification DifferentReceipt
                    given Recorded
                      for "ABCDEFGHIJKLMNOPQRSTU"
                    when Record
                      id = "ABCDEFGHIJKLMNOPQRSTU"
                      generated receipt = "VWXYZabcdefghijklmnop"
                    then Recorded
                      for "ABCDEFGHIJKLMNOPQRSTU"
                slice StateChange UuidReceipt
                  event Issued
                  command Issue
                    id GeneratedId identifier
                    receipt GeneratedId generated
                    produces Issued
                      for id
                  specification MatchingUuidReceipt
                    given Issued
                      for "00112233-4455-7677-8899-aabbccddeeff"
                    when Issue
                      id = "00112233-4455-7677-8899-aabbccddeeff"
                      generated receipt = "00112233-4455-7677-8899-aabbccddeeff"
                    then error "A generated event source already has facts; retry the command with a fresh generated value."
                slice StateChange TextUuidReceipt
                  event Delivered
                  command Deliver
                    id String identifier
                    receipt GeneratedId generated
                    produces Delivered
                      for id
                  specification MatchingUuidTextReceipt
                    given Delivered
                      for "{00112233-4455-7677-8899-AABBCCDDEEFF}"
                    when Deliver
                      id = "{00112233-4455-7677-8899-AABBCCDDEEFF}"
                      generated receipt = "00112233-4455-7677-8899-aabbccddeeff"
                    then error "A generated event source already has facts; retry the command with a fresh generated value."
            """;

        protected override string ApplicationName => "ReceiptApp";
        protected override string ProjectFile => "ReceiptApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("stage-authored-v15", invoice_model.Compile(Source))];

        Task Because() => Verify();
    }
}
#endif
