// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_admitting_the_v21_golden_vector : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(21).Bytes.AsSpan()), []).Plan;
    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_read_the_full_v21_vector() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    [Fact] void should_run_a_nonempty_specification_set() => _report.Results.ShouldNotBeEmpty();
    [Fact] void should_not_refuse_the_audited_version() => _report.Results.Any(result => result.Unsupported?.Details.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal) == true).ShouldBeFalse();
    [Theory]
    [InlineData("TenantOrderId")]
    async Task should_admit_an_unused_rule_concept(string conceptName)
    {
        var concept = _plan.Model.Application.Concepts.Single(concept => concept.Name == conceptName);

        // An unused generation body must not block unrelated billing specifications.
        const string source = conformance.when_running_inline_billing_validation.Source;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("V21Refusals"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("refusals"), "refusals", "Refusals.play", source);
        var compilation = new SemanticModelCompiler().Compile("V21Refusals", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var application = compilation.Value.Model.Application;
        var model = ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application with
        {
            Concepts = application.Concepts.Add(concept)
        });
        var plan = SemanticExecutionPlan.Compile(model, []).Plan;
        var report = await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken);
        report.Results.ShouldNotBeEmpty();
        Assert.All(report.Results, result =>
        {
            result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
            result.Unsupported.ShouldBeNull();
        });
    }
}
