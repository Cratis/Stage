// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_whole_number_specifications(when_executing_generated_whole_number_specifications.context fixture) : IClassFixture<when_executing_generated_whole_number_specifications.context>
{
    [Fact] void should_build_without_warnings() => fixture.BuildWarningLines.ShouldEqual(string.Empty);
    [Fact] void should_round_trip_both_large_whole_numbers_through_command_event_and_read_model() => fixture.Test.ShouldContain("Passed!");
    [Fact] void should_execute_both_authored_cases() => fixture.Cases.ShouldContainOnly("when_recording_positive", "when_recording_negative", "when_recording_positive_is_projected", "when_recording_negative_is_projected");
    [Fact] void should_pass_every_generated_assertion() => fixture.Outcomes.ShouldContainOnly("Passed");

    public class context : a_generated_application
    {
        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(a_whole_number_model.Compile(SemanticVersion.V11));

        async Task Because()
        {
            Build = await Run("whole-number-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "-p:NoWarn=CS7022", "--nologo");
            Test = await Run("whole-number-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--filter", "FullyQualifiedName~when_recording_", "--logger", "trx;LogFileName=whole-numbers.trx", "--results-directory", _evidence.FullName);
            var results = XDocument.Load(Path.Combine(_evidence.FullName, "whole-numbers.trx"));
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            Cases = [.. results.Descendants(ns + "TestMethod").Select(method => ((string)method.Attribute("className")!).Split('.')[^1]).Distinct(StringComparer.Ordinal)];
            Outcomes = [.. results.Descendants(ns + "UnitTestResult").Select(result => (string)result.Attribute("outcome")!).Distinct(StringComparer.Ordinal)];
        }

        public string Build { get; private set; } = null!;
        public string Test { get; private set; } = null!;
        public string[] Cases { get; private set; } = [];
        public string[] Outcomes { get; private set; } = [];
        public string BuildWarningLines => BuildWarnings(Build);
    }
}
#endif
