// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_slices_with_unrenderable_siblings : a_register_project_render_request
{
    readonly List<(ExecutableSemanticModel Model, SemanticExecutionPlan ExecutionPlan, SemanticSlice Sibling, SemanticId Artifact, string Code)> _cases = [];
    readonly List<(ArtifactRenderPlan Clean, ArtifactRenderPlan Refused, SemanticId Artifact, string Code)> _plans = [];

    void Establish()
    {
        (string Source, SemanticVersion Version, string Code)[] siblings =
        [
            ("""
                slice Automation Unsupported
                  reaction ReminderScheduler
                    when ProjectRegistered
                      projectId
                      produces ReminderScheduled
                        scheduledAt = $context.occurred
                  event ReminderScheduled
                    scheduledAt DateTime
            """, SemanticVersion.V6, "STAGE-ESM-024"),
            ("""
                slice StateView Unsupported
                  event InvoiceRemoved
                    invoiceId String
                  readmodel InvoiceView
                    invoiceId String
                  query InvoiceById => InvoiceView?
                    by invoiceId String
                  projection Invoices => InvoiceView
                    remove with InvoiceRemoved key invoiceId
                  specification RemovingAnInvoice
                    given readmodel InvoiceView
                      invoiceId = "first"
                    when append InvoiceRemoved
                      invoiceId = "first"
                    then no readmodel InvoiceView for "first"
            """, SemanticVersion.V5, "STAGE-ESM-027"),
            ("""
                slice StateChange Unsupported
                  command RegisterOtherProject
                    projectId ProjectId generated identifier
                    name String
                    produces event OtherProjectRegistered
                      name String = name
            """, SemanticVersion.V7, "STAGE-ESM-028"),
            ("""
                slice StateChange Unsupported
                  command RenameOtherProject
                    projectId ProjectId identifier
                    name String
                    produces event OtherProjectRenamed
                      for projectId
                      name String = name
                    returns name
            """, SemanticVersion.V7, "STAGE-ESM-029")
        ];
        var form = Corpus.SourceForms.Single(candidate => candidate.Name == "single");
        var document = form.Documents.Single();
        foreach (var sibling in siblings)
        {
            var model = Compile(form with
            {
                Documents = [document with { Bytes = [.. Encoding.UTF8.GetBytes(document.Text + Environment.NewLine + sibling.Source)] }]
            }).Model;
            model.SemanticVersion.ShouldEqual(sibling.Version);
            var slice = model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Name == "Unsupported");
            var artifact = sibling.Code switch
            {
                "STAGE-ESM-024" => slice.Reactions.Single().Id,
                "STAGE-ESM-027" => slice.Specifications.Single().Id,
                _ => slice.Commands.Single().Id
            };
            var compilation = SemanticExecutionPlan.Compile(model);
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Issues));
            _cases.Add((model, compilation.Plan!, slice, artifact, sibling.Code));
        }
    }

    void Because()
    {
        foreach (var item in _cases)
        {
            var clean = CratisRendering.Plan(item.Model, item.ExecutionPlan, new(ArtifactRenderScopeKind.Slice, _registerProject.Id), _options);
            var refused = CratisRendering.Plan(item.Model, item.ExecutionPlan, new(ArtifactRenderScopeKind.Slice, item.Sibling.Id), _options);
            _plans.Add((clean, refused, item.Artifact, item.Code));
        }
    }

    [Fact] void should_cover_every_later_version_refusal() => _plans.Select(plan => plan.Code).ShouldContainOnly(["STAGE-ESM-024", "STAGE-ESM-027", "STAGE-ESM-028", "STAGE-ESM-029"]);
    [Fact] void should_render_the_clean_slice_in_every_model() => _plans.All(plan => plan.Clean.Success).ShouldBeTrue();
    [Fact] void should_report_no_diagnostics_for_the_clean_slice() => _plans.SelectMany(plan => plan.Clean.Diagnostics).ShouldBeEmpty();
    [Fact] void should_emit_the_clean_command() => _plans.All(plan => plan.Clean.Artifacts.Any(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs")).ShouldBeTrue();
    [Fact] void should_not_emit_the_unselected_sibling() => _plans.SelectMany(plan => plan.Clean.Artifacts).Any(artifact => artifact.RelativePath.StartsWith("Projects/Registration/Unsupported/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_refuse_the_offending_slice_in_every_model() => _plans.All(plan => !plan.Refused.Success).ShouldBeTrue();
    [Fact] void should_report_only_the_expected_refusal() => _plans.All(plan => plan.Refused.Diagnostics.Length > 0 && plan.Refused.Diagnostics.All(diagnostic => diagnostic.Code == plan.Code)).ShouldBeTrue();
    [Fact] void should_name_the_offending_declaration() => _plans.All(plan => plan.Refused.Diagnostics.Any(diagnostic => diagnostic.Artifact == plan.Artifact)).ShouldBeTrue();
    [Fact] void should_emit_no_partial_artifacts_for_the_offending_slice() => _plans.SelectMany(plan => plan.Refused.Artifacts).ShouldBeEmpty();
}
