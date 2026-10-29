// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_slices_with_unrenderable_siblings : a_register_project_render_request
{
    const string ResponseSibling = """
            slice StateChange Unsupported
              command RenameOtherProject
                projectId ProjectId identifier
                toString String
                produces event OtherProjectRenamed
                  for projectId
                returns
                  projectId = projectId
                  toString = toString
        """;

    const string InvoiceSibling = """
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
                    given InvoiceRemoved
                      invoiceId = "first"
                    when append InvoiceRemoved
                      invoiceId = "first"
                    then no readmodel InvoiceView for "first"
            """;

    readonly List<(ExecutableSemanticModel Model, SemanticExecutionPlan ExecutionPlan, SemanticSlice Sibling, SemanticId Artifact, string Code)> _cases = [];
    readonly List<(ArtifactRenderPlan Clean, ArtifactRenderPlan Refused, SemanticId Artifact, string Code)> _plans = [];

    void Establish()
    {
        (string Source, SemanticVersion Version, string Code)[] siblings =
        [
            ("""
                slice Automation Unsupported
                  reaction ReminderScheduler
                    every 1 minutes
                      produces ReminderScheduled
                        for "reminders"
                        scheduledAt = $context.occurred
                  event ReminderScheduled
                    scheduledAt DateTime
            """, SemanticVersion.V6, "STAGE-ESM-024"),
            (InvoiceSibling, SemanticVersion.V5, string.Empty),
            (InvoiceSibling.Replace("remove with InvoiceRemoved key invoiceId", "remove with InvoiceRemoved", StringComparison.Ordinal), SemanticVersion.V5, "STAGE-ESM-051"),
            ("""
                slice StateChange Unsupported
                  command RegisterOtherProject
                    projectId ProjectId generated identifier
                    name String
                    produces OtherProjectRegistered
                      name = name
                  event OtherProjectRegistered
                    name String
            """, SemanticVersion.V7, "STAGE-ESM-038"),
            (ResponseSibling, SemanticVersion.V7, "STAGE-ESM-038")
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
                "" or "STAGE-ESM-051" => slice.Specifications.Single().Id,
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

    [Fact] void should_cover_every_later_version_refusal() => _plans.Where(plan => plan.Code != string.Empty).Select(plan => plan.Code).ShouldContainOnly(["STAGE-ESM-024", "STAGE-ESM-051", "STAGE-ESM-038", "STAGE-ESM-038"]);
    [Fact] void should_render_the_clean_slice_in_every_model() => _plans.TrueForAll(plan => plan.Clean.Success).ShouldBeTrue();
    [Fact] void should_report_no_diagnostics_for_the_clean_slice() => _plans.SelectMany(plan => plan.Clean.Diagnostics).WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
    [Fact] void should_emit_the_clean_command() => _plans.TrueForAll(plan => plan.Clean.Artifacts.Any(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs")).ShouldBeTrue();
    [Fact] void should_not_emit_the_unselected_sibling() => _plans.SelectMany(plan => plan.Clean.Artifacts).Any(artifact => artifact.RelativePath.StartsWith("Projects/Registration/Unsupported/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_refuse_only_production_gaps() => _plans.TrueForAll(plan => plan.Refused.Success == (plan.Code is "" or "STAGE-ESM-051")).ShouldBeTrue();
    [Fact] void should_render_the_payload_keyed_invoice_specification() => _plans.Single(plan => plan.Code == string.Empty).Refused.Artifacts.SelectMany(artifact => artifact.Sources).ShouldContain(_plans.Single(plan => plan.Code == string.Empty).Artifact);
    [Fact] void should_report_no_diagnostics_for_the_payload_keyed_invoice() => _plans.Single(plan => plan.Code == string.Empty).Refused.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
    [Fact] void should_report_the_expected_refusal() => _plans.Where(plan => plan.Code != string.Empty).All(plan => plan.Refused.Diagnostics.Any(diagnostic => diagnostic.Code == plan.Code && diagnostic.Artifact == plan.Artifact)).ShouldBeTrue();
    [Fact] void should_identify_the_observable_source_refusal() => _plans.Single(plan => plan.Code == "STAGE-ESM-051").Refused.Refusals.Single().Artifact.ShouldEqual(_plans.Single(plan => plan.Code == "STAGE-ESM-051").Artifact);
    [Fact] void should_preserve_the_observable_source_refusal_severity() => _plans.Single(plan => plan.Code == "STAGE-ESM-051").Refused.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-051").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_explain_the_observable_source_refusal() => _plans.Single(plan => plan.Code == "STAGE-ESM-051").Refused.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-051").Message.ShouldContain("given event 'InvoiceRemoved' has no event source");
    [Fact] void should_not_blanket_refuse_scalar_absence() => _plans.Single(plan => plan.Code == "STAGE-ESM-051").Refused.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-027");
    [Fact] void should_emit_no_artifacts_for_production_gaps() => _plans.Where(plan => plan.Code is not ("" or "STAGE-ESM-051")).SelectMany(plan => plan.Refused.Artifacts).ShouldBeEmpty();
    [Fact] void should_omit_only_the_unsupported_specification() => _plans.Single(plan => plan.Code == "STAGE-ESM-051").Refused.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_plans.Single(plan => plan.Code == "STAGE-ESM-051").Artifact);
}
