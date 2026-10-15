// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_preserving_optional_lookup_plan_bytes : Specification
{
    ArtifactRenderPlan _plan = null!;
    ExecutableSemanticModel _model = null!;

    void Because()
    {
        var source = a_validated_query_key_model.Source.Replace("      query InvoicesById => observable InvoiceSummary[]\n        by invoiceId InvoiceId\n", string.Empty, StringComparison.Ordinal);
        _model = invoice_model.Compile(source);
        _model.Application.EventSources.ShouldBeEmpty();
        _plan = invoice_model.Plan(_model);
    }

    [Fact] void should_admit_the_lookup_only_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_preserve_every_planned_artifact_byte_without_event_sources() => _plan.Digest.ShouldEqual("96b91002dcfc4270cd52d3aa8fb47c837a536c37ebe6d8f245586cf682d0e8d5");
    [Fact] void should_change_only_concept_query_arguments_runtime_pins_slice_sources_and_optional_stage_import_from_the_previous_frozen_plan() => PreviousRealizationDigest().ShouldEqual("c15dc41dbf79c1f851002faf0d750041886bd7aad83ba27d4c898616339e4790");

    string PreviousRealizationDigest() => ArtifactRenderPlan.ComputeDigest(
        _plan.Target,
        // The target version is the scaffold's Cratis package pin, so restore it alongside the project bytes.
        _plan.TargetVersion.Replace("22.100.1", "22.87.0", StringComparison.Ordinal),
        _plan.Renderer,
        _plan.RendererVersion,
        _plan.ApplicationName,
        [.. _plan.Artifacts.Where(artifact => !artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => artifact.Kind == PlannedArtifactKind.Text
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                PreviousRealization(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
                previous_slice_sources.Of(artifact, _model))
            : artifact)],
        _plan.Diagnostics);

    // CPM keeps the backend Arc pins in Directory.Packages.props; frontend files may use the same version independently.
    static string PreviousRealization(string relativePath, string text)
    {
        var previous = PreviousFrontendEntryPoint.Restore(relativePath, text).Replace("19.66.0", "19.55.2", StringComparison.Ordinal).Replace("4.30.0", "4.22.1", StringComparison.Ordinal);
        if (relativePath.EndsWith("Lookup.cs", StringComparison.Ordinal))
        {
            // Restore the frozen #350 primitive query signature and conversion, not the read-model property.
            previous = previous.Replace("IReadModels readModels, global::Invoices.Common.InvoiceId invoiceId)", "IReadModels readModels, string invoiceId)", StringComparison.Ordinal)
                .Replace("(global::Cratis.Chronicle.Events.EventSourceId)invoiceId)", "(global::Cratis.Chronicle.Events.EventSourceId)new global::Invoices.Common.InvoiceId(invoiceId))", StringComparison.Ordinal);
        }

        return relativePath.EndsWith("Directory.Packages.props", StringComparison.Ordinal)
            ? previous.Replace("22.100.1", "22.87.0", StringComparison.Ordinal)
            : previous;
    }
}
