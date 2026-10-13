// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_canonical_generated_values(when_comparing_canonical_generated_values.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_canonical_generated_values.context>
{
    [Fact] void should_compare_all_twelve_cases() => fixture.Outcomes.Length.ShouldEqual(12);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_preserve_canonical_bytes() => Assert.All(GeneratedValuesCorpus.V15.SourceForms, form =>
        Assert.True(SemanticModelSerializer.Serialize(context.Model(form)).AsSpan().SequenceEqual(GeneratedValuesCorpus.V15.EsmBytes.AsSpan())));

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "GeneratedValues";
        protected override string ProjectFile => "GeneratedValues.csproj";
        protected override bool RequireStageExecution => true;
        protected override CanonicalCorpusVector Baseline => GeneratedValuesCorpus.V15;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => Baseline.SourceForms.Select(form => (form.Name, Model(form)));
        internal static ExecutableSemanticModel Model(CanonicalCorpusSourceForm form) => Compile(GeneratedValuesCorpus.V15, form);
        Task Because() => Verify();
    }
}
#endif
