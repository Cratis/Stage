// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
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
    [Fact] void should_preserve_every_planned_artifact_byte_without_event_sources() => _plan.Digest.ShouldEqual("d7e1e2eefd47eb57decda700256f06e1996cdb248e714363a8bcab6a21c7708a");
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_plan() => PreviousPinDigest().ShouldEqual("405e4e73c50c892994769e5bc5aac6c803cbe4e9052225baa9a8c45a06a0f3d9");

    string PreviousPinDigest() => ArtifactRenderPlan.ComputeDigest(
        _plan.Target,
        // The target version is the scaffold's Cratis package pin, so restore it alongside the project bytes.
        _plan.TargetVersion.Replace("22.95.0", "22.87.0", StringComparison.Ordinal),
        _plan.Renderer,
        _plan.RendererVersion,
        _plan.ApplicationName,
        [.. _plan.Artifacts.Select(artifact => artifact.Kind == PlannedArtifactKind.Text
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                PreviousPins(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
                artifact.Sources)
            : artifact)],
        _plan.Diagnostics);

    // Only the generated backend project carries the scaffold's Arc pin; other files may name 22.95.0 for unrelated packages.
    static string PreviousPins(string relativePath, string text)
    {
        var previous = text.Replace("19.59.6", "19.55.2", StringComparison.Ordinal);

        return relativePath.EndsWith(".csproj", StringComparison.Ordinal)
            ? previous.Replace("22.95.0", "22.87.0", StringComparison.Ordinal)
            : previous;
    }
}
