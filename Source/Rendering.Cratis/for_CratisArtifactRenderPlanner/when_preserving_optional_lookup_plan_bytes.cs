// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
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

    // Re-frozen for #656: generated Scene layouts are empty and screen layout names defer to the blueprint.
    // Re-frozen for Cratis/Stage#707: the default scaffold no longer installs or configures PrimeReact (package.json Prime deps, main.tsx
    // provider/theme/licence, vite styledPrimeReact/licence, primeicons) and pins Scene 4.80.2 and Components 4.34.0.
    // Re-frozen for #437: only the Matching package pin and host reference are added.
    // Re-frozen for Cratis/Stage#816 and Cratis/Stage#757: only src/bindings.ts command imports and the package.json fundamentals resolution change.
    // Re-frozen for #660: the only artifact-byte delta in all four views is CratisReadProtection=On in Directory.Build.props.
    // Re-frozen for #741 (.gitignore gains .yarn/) and #874 (Debug Chronicle.Testing telemetry dependency).
    // The attribution spec below restores both prior full-plan digests without changing any model or emitted C#.
    [Fact] void should_confine_changes_to_common_and_using_placement() => FrozenScaffoldPins.Digest(_plan, Legacy(QualifiedCommonOutput.Restore(_plan, () => invoice_model.Plan(_model)))).ShouldEqual("18eb418273cf8bb93a801f6175b60843e522d6bf8bef74f85a2396cb154915fa");

    // Re-frozen for #424: generated C# uses short type names and using directives; the qualified restore above still matches.
    // Re-frozen for Cratis/Stage#816 and Cratis/Stage#757: only src/bindings.ts command imports and the package.json fundamentals resolution change.
    [Fact] void should_preserve_every_planned_artifact_byte_without_event_sources() => FrozenScaffoldPins.Digest(_plan, Legacy(_plan.Artifacts)).ShouldEqual("3cfd6083f946c396d20139cfb86ffaffcf00cab073349313d269f0c7acb9af76");

    // Re-frozen when the license header became the caller's choice and specifications moved into the application project.
    // Re-frozen for #424: generated C# uses short type names and using directives.
    // Re-frozen for Cratis/Stage#816 (aliased Scene command imports in src/bindings.ts) and Cratis/Stage#757 (fundamentals resolution in package.json).
    [Fact] void should_emit_the_frozen_plan_bytes() => FrozenScaffoldPins.Digest(_plan).ShouldEqual("71e56b052e1b97bb63f6d6c49fcebd3d8277146ffd5a1663c4fc3b026d3ef9df");
    [Fact] void should_change_only_concept_query_arguments_runtime_pins_slice_sources_and_optional_stage_import_from_the_previous_frozen_plan() => PreviousRealizationDigest().ShouldEqual("0d710cac8514d07fcb08602ffc00bb303adaa9c470e867bd9e1d60bc17addd69");

    [Fact]
    void should_attribute_the_frozen_plan_changes_to_yarn_cache_and_debug_telemetry()
    {
        var beforeTelemetry = _plan.Artifacts.Select(artifact => artifact.RelativePath == "Directory.Packages.props" || artifact.RelativePath == "InvoiceApp.csproj"
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                string.Join('\n', Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Split('\n')
                    .Where(line => !line.Contains("Microsoft.Extensions.Telemetry.Abstractions", StringComparison.Ordinal))),
                artifact.Sources)
            : artifact).ToImmutableArray();
        beforeTelemetry.Where(artifact => _plan.Artifacts.Single(current => current.RelativePath == artifact.RelativePath).Sha256 != artifact.Sha256)
            .Select(artifact => artifact.RelativePath).ShouldContainOnly("Directory.Packages.props", "InvoiceApp.csproj");
        FrozenScaffoldPins.Digest(_plan, beforeTelemetry).ShouldEqual("c8291ec140b966ab17c528c524fc2374212a7b52fe113e36a7f02867e7255dca");
        var beforeYarn = beforeTelemetry.Select(artifact => artifact.RelativePath == ".gitignore"
            ? PlannedArtifact.CreateText(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace(".yarn/\n", string.Empty, StringComparison.Ordinal), artifact.Sources)
            : artifact).ToImmutableArray();
        FrozenScaffoldPins.Digest(_plan, beforeYarn).ShouldEqual("26281c694ecf92f5be19ec180d5379ee344de9741a42b891c052d8a21baceb78");
    }

    string PreviousRealizationDigest() => ArtifactRenderPlan.ComputeDigest(
        _plan.Target,

        // The target version is the scaffold's Cratis package pin, so restore it alongside the project bytes.
        "22.87.0",
        _plan.Renderer,
        _plan.RendererVersion,
        _plan.ApplicationName,
        [.. Legacy(QualifiedCommonOutput.Restore(_plan, () => invoice_model.Plan(_model))).Where(artifact => !artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal)).Select(artifact => artifact.Kind == PlannedArtifactKind.Text
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                PreviousRealization(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
                previous_slice_sources.Of(artifact, _model))
            : artifact)],
        _plan.Diagnostics);

    static IEnumerable<PlannedArtifact> Legacy(IEnumerable<PlannedArtifact> artifacts) => SeparateSpecificationsLayout.RestoreWithLicenseHeader(artifacts, "InvoiceApp", "Invoices");

    // CPM keeps the backend Arc pins in Directory.Packages.props; frontend files may use the same version independently.
    static string PreviousRealization(string relativePath, string text)
    {
        var previous = FrozenScaffoldPins.Normalize(relativePath, PreviousFrontendEntryPoint.Restore(relativePath, text), previous: true);
        if (relativePath.EndsWith("Lookup.cs", StringComparison.Ordinal))
        {
            // Restore the frozen #350 primitive query signature and conversion, not the read-model property.
            previous = previous.Replace("IReadModels readModels, global::Invoices.Common.InvoiceId invoiceId)", "IReadModels readModels, string invoiceId)", StringComparison.Ordinal)
                .Replace("(global::Cratis.Chronicle.Events.EventSourceId)invoiceId)", "(global::Cratis.Chronicle.Events.EventSourceId)new global::Invoices.Common.InvoiceId(invoiceId))", StringComparison.Ordinal);
        }

        // Cratis/Stage#816 and Cratis/Stage#757 are the only changes to src/bindings.ts and package.json since this plan was frozen.
        return PreviousSceneBindingsAndResolutions.Restore(relativePath, previous);
    }
}
