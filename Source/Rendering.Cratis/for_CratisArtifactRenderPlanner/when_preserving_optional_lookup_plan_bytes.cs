// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_preserving_optional_lookup_plan_bytes : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = a_validated_query_key_model.Source.Replace("      query InvoicesById => observable InvoiceSummary[]\n        by invoiceId InvoiceId\n", string.Empty, StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        model.Application.EventSources.ShouldBeEmpty();
        _plan = invoice_model.Plan(model);
    }

    [Fact] void should_admit_the_lookup_only_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_preserve_every_planned_artifact_byte_without_event_sources() => _plan.Digest.ShouldEqual("80aa577f65bfa20394a6f93e8e032ba05e18ece81d080069ff49372c3626a9a8");
    [Fact] void should_change_only_concept_query_arguments_runtime_pins_and_optional_stage_import_from_the_previous_frozen_plan() => PreviousRealizationDigest().ShouldEqual("405e4e73c50c892994769e5bc5aac6c803cbe4e9052225baa9a8c45a06a0f3d9");

    string PreviousRealizationDigest() => ArtifactRenderPlan.ComputeDigest(
        _plan.Target,
        // The target version is the scaffold's Cratis package pin, so restore it alongside the project bytes.
        _plan.TargetVersion.Replace("22.97.0", "22.87.0", StringComparison.Ordinal),
        _plan.Renderer,
        _plan.RendererVersion,
        _plan.ApplicationName,
        [.. _plan.Artifacts.Select(artifact => artifact.Kind == PlannedArtifactKind.Text
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                PreviousRealization(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
                artifact.Sources)
            : artifact)],
        _plan.Diagnostics);

    // Only the generated backend project carries the scaffold's Arc pin; other files may name 22.97.0 for unrelated packages.
    static string PreviousRealization(string relativePath, string text)
    {
        var previous = PreviousFrontendEntryPoint.Restore(relativePath, text).Replace("19.64.1", "19.55.2", StringComparison.Ordinal);
        if (relativePath.EndsWith("Lookup.cs", StringComparison.Ordinal))
        {
            // Restore the frozen #350 primitive query signature and conversion, not the read-model property.
            previous = previous.Replace("IReadModels readModels, global::Invoices.Common.InvoiceId invoiceId)", "IReadModels readModels, string invoiceId)", StringComparison.Ordinal)
                .Replace("(global::Cratis.Chronicle.Events.EventSourceId)invoiceId)", "(global::Cratis.Chronicle.Events.EventSourceId)new global::Invoices.Common.InvoiceId(invoiceId))", StringComparison.Ordinal);
        }

        return relativePath.EndsWith(".csproj", StringComparison.Ordinal)
            ? previous.Replace("22.97.0", "22.87.0", StringComparison.Ordinal)
            : previous;
    }
}
