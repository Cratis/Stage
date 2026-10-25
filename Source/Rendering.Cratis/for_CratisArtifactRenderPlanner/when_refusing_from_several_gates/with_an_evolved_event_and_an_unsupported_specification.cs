// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_an_evolved_event_and_an_unsupported_specification : given.a_collecting_render_request
{
    SemanticId _event;
    SemanticId _specification;

    void Establish()
    {
        var declarations = string.Join('\n', Enumerable.Range(1, 2).Select(revision =>
            $"      event ProjectRegistered generation {revision}\n        projectId ProjectId\n        name ProjectName"));
        var source = Source.Replace("      event ProjectRegistered\n        projectId ProjectId\n        name ProjectName", declarations, StringComparison.Ordinal)
            .Replace(
                "      specification RegisteringAProject\n",
                """
                      specification RegisteringAProject
                        given readmodel ProjectSummary
                          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                          name = "Screenplay"
                """ + "\n",
                StringComparison.Ordinal);
        var model = CompileEvolvedModel(source);
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _request = _request with { Model = model, ExecutionPlan = execution.Plan! };
        var slice = _request.Model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Kind == SemanticSliceKind.StateChange);
        _event = slice.Events.Single().Id;
        _specification = slice.Specifications.Single(specification => specification.Name == "RegisteringAProject").Id;
    }

    void Because() => _plan = _planner.Plan(_request);

    static ExecutableSemanticModel CompileEvolvedModel(string source)
    {
        var form = Corpus.SourceForms.Single(candidate => candidate.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        form = form with { Documents = [form.Documents.Single() with { Bytes = [.. Encoding.UTF8.GetBytes(source)] }] };

        // Bootstrap the current addresses, then advance the persisted catalog without replacing its identities.
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
        return Compile(form with { IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(advanced)] }).Model;
    }

    [Fact] void should_report_the_event_revision() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-026" && diagnostic.Artifact == _event);
    [Fact] void should_also_report_specification_admission() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == _specification);
    [Fact] void should_omit_refused_parts_and_compile_retained_artifacts() => global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.OmittedParts(_plan);
}
