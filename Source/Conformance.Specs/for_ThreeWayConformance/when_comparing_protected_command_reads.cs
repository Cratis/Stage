// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_protected_command_reads(when_comparing_protected_command_reads.context fixture) : IClassFixture<when_comparing_protected_command_reads.context>
{
    // The original thirteen specifications retain parity in every form. The six decision-0091
    // specifications expose the renderer gap tracked by Stage#503; neither engine may diverge.
    const int OutcomeCount = 76;

    static string[] RendererDifferences => [.. ProtectedCommandReadsCorpus.V25.SourceForms.SelectMany(form => refused_read_keys.Names.Select(name =>
        $"{form.Name}/{name}: Reference=Passed, Stage=Passed, Rendered=Failed"))];

    [Fact] void should_record_only_the_exact_refused_key_renderer_differences() => fixture.Differences.ShouldContainOnly(RendererDifferences);
    [Fact] void should_execute_each_refused_key_in_every_form() => fixture.Outcomes.Intersect(RendererDifferences, StringComparer.Ordinal).Count().ShouldEqual(24);
    [Fact] void should_compare_every_specification_of_every_form() => fixture.Outcomes.Length.ShouldEqual(OutcomeCount);
    [Fact] void should_pass_every_other_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(52);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "ProtectedReads";
        protected override string ProjectFile => "ProtectedReads.csproj";
        protected override CanonicalCorpusVector? Baseline => ProtectedCommandReadsCorpus.V25;
        protected override bool RequireStageExecution => true;
        protected override bool ExpectFailedGeneratedTest => true;
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
