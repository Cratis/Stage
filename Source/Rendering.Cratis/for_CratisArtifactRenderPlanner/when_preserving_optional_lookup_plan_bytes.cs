// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    // Re-frozen for #734/#741/#757: only .gitignore, .yarnrc.yml and package.json change; lookup and backend bytes are unchanged.
    [Fact] void should_confine_changes_to_common_and_using_placement() => FrozenScaffoldPins.Digest(_plan, Legacy(QualifiedCommonOutput.Restore(_plan, () => invoice_model.Plan(_model)))).ShouldEqual("3895199a804df03ac592e0732ef2207503742f0968168e662681f61560ccc5af");

    // Re-frozen for #424: generated C# uses short type names and using directives; the qualified restore above still matches.
    [Fact] void should_preserve_every_planned_artifact_byte_without_event_sources() => FrozenScaffoldPins.Digest(_plan, Legacy(_plan.Artifacts)).ShouldEqual("2065f7e7025f8a25dd89d2cc5ca085f0e331ae33e58e71411bdb1c4cd711a36d");

    // Re-frozen when the license header became the caller's choice and specifications moved into the application project.
    // Re-frozen for #424: generated C# uses short type names and using directives.
    [Fact] void should_emit_the_frozen_plan_bytes() => FrozenScaffoldPins.Digest(_plan).ShouldEqual("720183ffd679a2a072451f00cfe48b2e971cc29ff3ae534e880aca3099ec0ded");

    // PreviousRealization restores the older Yarn configuration; #757's new fundamentals resolution remains intentional.
    [Fact] void should_change_only_concept_query_arguments_runtime_pins_slice_sources_and_optional_stage_import_from_the_previous_frozen_plan() => PreviousRealizationDigest().ShouldEqual("bcc8a2d1d632a7b20e38a7275aba0ff03496dbf7943510e5fc87111abb89e721");

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

        return previous;
    }
}
