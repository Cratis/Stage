// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_numbered_enumeration_specifications : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish()
    {
        var corpus = EnumMemberNumbersCorpus.V12;
        var form = corpus.SourceForms.Single(source => source.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        compilation.Success.ShouldBeTrue();
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(compilation.Value!.Model))).Plan!;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_admit_v12_after_strict_roundtrip() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V12);
    [Fact] void should_run_all_source_backed_cases() => _report.Results.Count.ShouldEqual(3);
    [Fact] void should_preserve_name_based_outcomes() => _report.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly(SemanticSpecificationOutcome.Passed);
}
