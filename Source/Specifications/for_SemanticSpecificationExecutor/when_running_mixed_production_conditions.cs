// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_mixed_production_conditions : Specification
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var source = string.Join('\n', GeneratedValuesCorpus.V15.SourceForms.Single(form => form.Name == "single").Documents.Select(document => document.Text))
            .Replace("produces when enabled == true\n          Initialized", "produces when enabled != true\n          Initialized", StringComparison.Ordinal)
            .Replace("        then Initialized\n          for \"00112233-4455-7677-8899-aabbccddeeff\"\n", string.Empty, StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Mixed"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("mixed"), "mixed", "Mixed.play", source);
        var compilation = new SemanticModelCompiler().Compile("Mixed", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var specification = plan.Specifications.Values.Single(specification => specification.Name == "SeveralInitialFacts");
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_append_only_the_taken_production() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
