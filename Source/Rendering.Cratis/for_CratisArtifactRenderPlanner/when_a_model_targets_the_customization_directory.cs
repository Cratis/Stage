// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_model_targets_the_customization_directory : Specification
{
    readonly List<ArtifactRenderPlan> _rejected = [];
    ArtifactRenderPlan _nested = null!;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);
        foreach (var name in new[] { "Customizations", "customizations", "CUSTOMIZATIONS" })
        {
            var model = invoice_model.Compile(source.Replace("module Billing", $"module {name}", StringComparison.Ordinal));
            _rejected.Add(invoice_model.Plan(model));
            _rejected.Add(invoice_model.Plan(model, new(ArtifactRenderScopeKind.Slice, model.Application.Modules.Single().Features.Single().Slices.Single().Id)));
        }

        _nested = invoice_model.Plan(invoice_model.Compile(source.Replace("feature Invoicing", "feature Customizations", StringComparison.Ordinal)));
    }

    [Fact] void should_check_application_and_slice_scopes_for_every_casing() => _rejected.Count.ShouldEqual(6);
    [Fact] void should_mark_every_conflicting_plan_partial() => _rejected.TrueForAll(plan => plan.Outcome == ArtifactRenderOutcome.Partial).ShouldBeTrue();
    [Fact] void should_keep_the_other_artifacts() => _rejected.TrueForAll(plan => !plan.Artifacts.IsEmpty).ShouldBeTrue();
    [Fact] void should_omit_reserved_artifacts_before_creating_the_plan() => _rejected.TrueForAll(plan => plan.Artifacts.All(artifact => !artifact.RelativePath.StartsWith("Customizations/", StringComparison.OrdinalIgnoreCase))).ShouldBeTrue();
    [Fact] void should_warn_about_the_reserved_directory() => _rejected.TrueForAll(plan => plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-CRATIS-005" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning)).ShouldBeTrue();
    [Fact] void should_record_the_omitted_owners() => _rejected.TrueForAll(plan => plan.ImplementationGaps.Any(gap => gap.Kind == ArtifactImplementationGapKind.Rendering)).ShouldBeTrue();
    [Fact] void should_mark_the_output_not_runnable() => _rejected.TrueForAll(plan => plan.Artifacts.Any(artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md")).ShouldBeTrue();
    [Fact] void should_allow_the_same_name_below_a_different_root() => _nested.Success.ShouldBeTrue();
}
