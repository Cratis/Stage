// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

using static Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.ThreeWayOutcomes;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_legacy_corpus_specifications(when_comparing_legacy_corpus_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_legacy_corpus_specifications.context>
{
    [Fact] void should_compare_every_specification_in_every_physical_form() => Assert.True(fixture.Outcomes.Length == RegisterProjectCorpus.LegacyV1.SourceForms.Length * RegisterProjectCorpus.LegacyV1.SpecificationExpectations.Length, string.Join(Environment.NewLine, fixture.Outcomes));
    [Fact] void should_match_all_three_executions() => Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Projects";
        protected override string ProjectFile => "BackendHost.csproj";
        protected override CanonicalCorpusVector Baseline => RegisterProjectCorpus.LegacyV1;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => RegisterProjectCorpus.LegacyV1.SourceForms.Select(form => (form.Name, Compile(RegisterProjectCorpus.LegacyV1, form)));
        Task Because() => Verify();
    }
}

public class when_comparing_v2_corpus_specifications(when_comparing_v2_corpus_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_v2_corpus_specifications.context>
{
    [Fact] void should_compare_every_specification_in_every_physical_form() => Assert.True(fixture.Outcomes.Length == RegisterProjectCorpus.V2.SourceForms.Length * RegisterProjectCorpus.V2.SpecificationExpectations.Length, string.Join(Environment.NewLine, fixture.Outcomes));
    [Fact] void should_match_all_three_executions() => Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Projects";
        protected override string ProjectFile => "BackendHost.csproj";
        protected override CanonicalCorpusVector Baseline => RegisterProjectCorpus.V2;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => RegisterProjectCorpus.V2.SourceForms.Select(form => (form.Name, Compile(RegisterProjectCorpus.V2, form)));
        Task Because() => Verify();
    }
}

public class when_comparing_inline_billing_specifications(when_comparing_inline_billing_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_inline_billing_specifications.context>
{
    [Fact] void should_compare_all_three_inline_specifications() => Assert.True(fixture.Outcomes.Length == 3, string.Join(Environment.NewLine, fixture.Outcomes));
    [Fact] void should_match_every_executed_path() => Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Billing";
        protected override string ProjectFile => "InvoiceApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("billing-validation", CompileBilling())];
        Task Because() => Verify();
    }
}

public class when_comparing_an_incorrect_inline_expectation(when_comparing_an_incorrect_inline_expectation.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_an_incorrect_inline_expectation.context>
{
    [Fact] void should_detect_the_failed_specification_on_every_executed_path() => Assert.True(fixture.Outcomes.Any(outcome => outcome.Contains("RegisteringAnInvoice: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal)), string.Join(Environment.NewLine, fixture.Outcomes));
    [Fact] void should_preserve_parity_for_the_other_specifications() => Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Billing";
        protected override string ProjectFile => "InvoiceApp.csproj";
        protected override bool ExpectFailedGeneratedTest => true;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("incorrect-billing-expectation", CompileWrongBilling())];
        Task Because() => Verify();
    }
}

internal static class ThreeWayOutcomes
{
    public static void Report(a_three_way_application fixture, ITestOutputHelper output)
    {
        foreach (var outcome in fixture.Outcomes)
        {
            output.WriteLine(outcome);
        }

        Assert.True(fixture.Differences.IsEmpty, string.Join(Environment.NewLine, fixture.Differences.Concat(fixture.Outcomes)));
    }
}
#endif
