// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_protected_command_reads(when_comparing_protected_command_reads.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_protected_command_reads.context>
{
    // Thirteen specifications in each of the four source forms of the canonical corpus.
    const int OutcomeCount = 52;

    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_compare_every_specification_of_every_form() => fixture.Outcomes.Length.ShouldEqual(OutcomeCount);
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(OutcomeCount);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "ProtectedReads";
        protected override string ProjectFile => "ProtectedReads.csproj";
        protected override CanonicalCorpusVector? Baseline => ProtectedCommandReadsCorpus.V25;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models =>
            ProtectedCommandReadsCorpus.V25.SourceForms.Select(form => (form.Name, Compile(ProtectedCommandReadsCorpus.V25, form)));

        Task Because()
        {
            foreach (var (name, source) in protected_command_read_probes.Files)
            {
                AddGeneratedSpecification(name, source);
            }

            return Verify();
        }
    }
}
#endif
