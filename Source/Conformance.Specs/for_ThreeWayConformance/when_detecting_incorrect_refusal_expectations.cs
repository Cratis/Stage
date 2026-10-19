// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_detecting_incorrect_refusal_expectations(when_detecting_incorrect_refusal_expectations.context fixture, ITestOutputHelper output) : IClassFixture<when_detecting_incorrect_refusal_expectations.context>
{
    [Fact] void should_fail_the_wrong_payload_on_all_three_paths() => fixture.Outcomes.ShouldContain(outcome => outcome.Contains("RecordValidation: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_fail_nonempty_acknowledgement_on_all_three_paths() => fixture.Outcomes.ShouldContain(outcome => outcome.Contains("NonemptyAcknowledgement: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_fail_only_the_incorrect_generated_assertions() => fixture.FailedAssertions.ShouldContainOnly(["should_append_event_1", "should_append_exactly_the_expected_events"]);
    [Fact] void should_preserve_parity_for_the_other_specification() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Refusals";
        protected override string ProjectFile => "Refusals.csproj";
        protected override bool RequireStageExecution => true;
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models =>
            [("wrong-payload", a_refusal_model.Compile(a_refusal_model.Source.Replace("reason = \"validation\"", "reason = \"wrong\"", StringComparison.Ordinal) + "\n      specification NonemptyAcknowledgement\n        when append QuietRequested\n          for \"one\"\n          id = \"one\"\n          amount = 1\n        then no events"))];
        public string[] FailedAssertions { get; private set; } = [];

        async Task Because()
        {
            await Verify();
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            FailedAssertions = [.. XDocument.Load(Path.Combine(_evidence.FullName, "parity.trx"))
                .Descendants(ns + "UnitTestResult").Where(result => result.Attribute("outcome")!.Value == "Failed")
                .Select(result => result.Attribute("testName")!.Value.Split('.')[^1])];
        }
    }
}
#endif
