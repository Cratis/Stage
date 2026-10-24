// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_concept_compliance_specifications(when_comparing_concept_compliance_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_concept_compliance_specifications.context>
{
    [Fact] void should_compare_every_source_form() => fixture.Outcomes.Length.ShouldEqual(4);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Invoices";
        protected override string ProjectFile => "InvoiceApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override CanonicalCorpusVector Baseline => ConceptComplianceCorpus.V13;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => Baseline.SourceForms.Select(form => (form.Name, Compile(Baseline, form)));
        Task Because() => Verify();
    }
}
#endif
