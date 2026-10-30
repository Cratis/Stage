// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_migration_removals : given.an_unsupported_event_request
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderProfile _profile = null!;
    ArtifactRenderPlan _baseline = null!;
    ArtifactRenderPlan _overlapping = null!;
    ArtifactRenderPlan _disjoint = null!;

    async Task Establish()
    {
        var source = when_migrating_a_classic_reducer.Source + "\n" +
            for_CratisArtifactRenderPlanner.given.invoice_model.Source("String", for_CratisArtifactRenderPlanner.given.invoice_model.TextSource, for_CratisArtifactRenderPlanner.given.invoice_model.OtherTextSource);
        _loaded = await when_rendering_a_pure_reducer.Load(source);
        var evidence = when_migrating_a_classic_reducer.Evidence(_loaded);
        _profile = CratisRendering.WithReducerMigration(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), "Projects", [evidence], true);
        _baseline = when_selecting_self_reducing_rendering.Plan(_loaded, _profile);
    }

    void Because()
    {
        var owners = _loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Events).ToArray();
        var overlapping = owners.Single(owner => owner.Name == "OrderPlaced");
        var original = overlapping.Properties;
        UnsupportedProperties(overlapping);
        _overlapping = when_selecting_self_reducing_rendering.Plan(_loaded, _profile);
        Replace(overlapping, "Properties", original);
        UnsupportedProperties(owners.Single(owner => owner.Name == "InvoiceIssued"));
        _disjoint = when_selecting_self_reducing_rendering.Plan(_loaded, _profile);
    }

    [Fact] void should_witness_a_successful_migration() => _baseline.Success.ShouldBeTrue();
    [Fact] void should_witness_a_real_migration_removal() => _baseline.RequiredRemovals.Length.ShouldEqual(1);
    [Fact] void should_refuse_when_the_removed_model_would_be_omitted() => _overlapping.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_clear_artifacts_for_the_overlapping_case() => _overlapping.Artifacts.ShouldBeEmpty();
    [Fact] void should_clear_removals_for_the_overlapping_case() => _overlapping.RequiredRemovals.ShouldBeEmpty();
    [Fact] void should_keep_the_original_error_for_the_overlapping_case() => _overlapping.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-012").ShouldBeTrue();
    [Fact] void should_render_around_a_disjoint_root() => _disjoint.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_keep_disjoint_migration_evidence_unchanged() => _disjoint.RequiredRemovals.SequenceEqual(_baseline.RequiredRemovals).ShouldBeTrue();
}
#endif
