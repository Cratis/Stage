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
    // Re-frozen for #741 (.gitignore gains .yarn/), #874 (Debug Chronicle.Testing telemetry dependency) and #734 (.yarnrc.yml preapproves @cratis/* packages).
    // The attribution spec below restores both prior full-plan digests without changing any model or emitted C#.
    [Fact] void should_confine_changes_to_common_and_using_placement() => FrozenScaffoldPins.Digest(_plan, Legacy(QualifiedCommonOutput.Restore(_plan, () => invoice_model.Plan(_model)))).ShouldEqual("75209133631d0da7812ee0725fa7ab1377932dad32ec4aa4d7c08541d153e9d6");

    // Re-frozen for #424: generated C# uses short type names and using directives; the qualified restore above still matches.
    // Re-frozen for Cratis/Stage#816 and Cratis/Stage#757: only src/bindings.ts command imports and the package.json fundamentals resolution change.
    [Fact] void should_preserve_every_planned_artifact_byte_without_event_sources() => FrozenScaffoldPins.Digest(_plan, Legacy(_plan.Artifacts)).ShouldEqual("3fc9f450cb8dcbfdb35ae6e6b3f255be5f8da85cbeb5db5baeb3a5152b600a96");

    // Re-frozen when the license header became the caller's choice and specifications moved into the application project.
    // Re-frozen for #424: generated C# uses short type names and using directives.
    // Re-frozen for Cratis/Stage#816 (aliased Scene command imports in src/bindings.ts) and Cratis/Stage#757 (fundamentals resolution in package.json).
    [Fact] void should_emit_the_frozen_plan_bytes() => FrozenScaffoldPins.Digest(_plan).ShouldEqual("ab3568dc74895c6bcf3107e8294bd6ef00bda632d9a2641cf366f06e9dd7b204");
    [Fact] void should_change_only_concept_query_arguments_runtime_pins_slice_sources_and_optional_stage_import_from_the_previous_frozen_plan() => PreviousRealizationDigest().ShouldEqual("bddf5f42b0dfe2a25f4e14c02d4c2789430bcc51734dae2f7d0f7c818021154b");

    [Fact]
    void should_attribute_the_frozen_plan_changes_to_yarn_cache_yarn_preapproval_and_debug_telemetry()
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
        FrozenScaffoldPins.Digest(_plan, beforeTelemetry).ShouldEqual("0a5a2ab389b0f05eefb97b929e3130d44fe93ed105b583af908bff6c58176275");
        var beforeYarn = beforeTelemetry.Select(artifact => artifact.RelativePath switch
        {
            ".gitignore" => PlannedArtifact.CreateText(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace(".yarn/\n", string.Empty, StringComparison.Ordinal), artifact.Sources),
            ".yarnrc.yml" => PlannedArtifact.CreateText(artifact.RelativePath, "nodeLinker: node-modules\n", artifact.Sources),
            _ => artifact
        }).ToImmutableArray();
        FrozenScaffoldPins.Digest(_plan, beforeYarn).ShouldEqual("ca384b0725d028c03ccf3cbf272f2da6cdfcb90c6c1d6ba7fa4d40854ab1df27");
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
