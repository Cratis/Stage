// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_the_canonical_v5_corpus : Specification
{
    ArtifactRenderRequest _request = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(ReadModelAbsenceCorpus.V5.EsmBytes.AsSpan());
        _request = new(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, new("Absence", "Absence")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_load_v5() => _request.Model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
    [Fact] void should_admit_seeded_absence() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_no_diagnostics() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_render_the_removal_only_projection() => _plan.Artifacts.ShouldContain(artifact => artifact.Sources.Contains(_request.Model.Application.Modules.Single().Features.Single().Slices.Single().Projections.Single().Id));
    [Fact] void should_cover_absence_specifications() => AbsenceSpecifications().ShouldNotBeEmpty();
    [Fact] void should_render_every_absence_specification() => AbsenceSpecifications().All(id => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(id))).ShouldBeTrue();
    [Fact]
    void should_admit_through_semantic_admission_as_well()
    {
        var context = new SemanticApplicationContext(_request, new("Absence", "Absence"));
        SemanticCratisAdmission.Evaluate(context, context.SelectedSlices()).ShouldBeEmpty();
    }

    IEnumerable<SemanticId> AbsenceSpecifications() => _request.Model.Application.Modules.SelectMany(module => module.Features)
        .SelectMany(AllSlices).SelectMany(slice => slice.Specifications)
        .Where(specification => !specification.ThenAbsentReadModels.IsDefaultOrEmpty).Select(specification => specification.Id);

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
