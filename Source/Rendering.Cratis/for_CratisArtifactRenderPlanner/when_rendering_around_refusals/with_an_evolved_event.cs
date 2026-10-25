// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_an_evolved_event : given.a_refused_specification
{
    void Establish()
    {
        var declarations = string.Join('\n', Enumerable.Range(1, 2).Select(revision =>
            $"      event ProjectRegistered generation {revision}\n        projectId ProjectId\n        name ProjectName"));
        var source = Source.Replace("      event ProjectRegistered\n        projectId ProjectId\n        name ProjectName", declarations, StringComparison.Ordinal);
        var form = Corpus.SourceForms.Single(candidate => candidate.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        form = form with { Documents = [form.Documents.Single() with { Bytes = [.. Encoding.UTF8.GetBytes(source)] }] };
        var current = Compile(form with
        {
            IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(catalog.Application))]
        }).Documents.IdentityCatalog;
        var eventAddress = catalog.EventContracts.Single(contract => contract.Address.Name == "ProjectRegistered").Address;
        var advanced = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            catalog,
            catalog.Revision,
            [.. current.Documents.Select(document => document.Key)],
            [.. current.Semantics.Select(assignment => assignment.Address)],
            [.. current.EventContracts.Select(contract => contract.Address)],
            [new(eventAddress, new(2))]).Catalog;
        var model = Compile(form with { IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(advanced)] }).Model;
        _request = OptIn(_original with { Model = model, ExecutionPlan = SemanticExecutionPlan.Compile(model).Plan! });
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_report_partial_rendering() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_keep_the_event_revision_refusal() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-026");
    [Fact] void should_omit_refused_parts_and_compile_retained_artifacts() => global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.Rejected(_plan, "STAGE-ESM-026");
    [Fact] void should_not_list_omissions() => _plan.Refusals.ShouldBeEmpty();
}
