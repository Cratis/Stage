// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_the_canonical_v5_corpus : Specification
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
    [Fact] void should_refuse_only_unsupported_fixtures() => _plan.Diagnostics.WithoutAuthenticatedDefaultNotes().Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_admit_the_removal_only_projection() => _plan.Diagnostics.Any(diagnostic => diagnostic.Artifact == _request.Model.Application.Modules.Single().Features.Single().Slices.Single().Projections.Single().Id).ShouldBeFalse();
    [Fact] void should_not_refuse_scalar_absence_itself() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-027");
    [Fact] void should_also_collect_specification_admission() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-011").Select(diagnostic => diagnostic.Artifact).ShouldContainOnly([.. AbsenceSpecifications()]);
    [Fact] void should_not_publish_a_partial_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_refuse_through_semantic_admission_as_well()
    {
        var context = new SemanticApplicationContext(_request, new("Absence", "Absence"));
        SemanticCratisAdmission.Evaluate(context, context.SelectedSlices()).WithoutAuthenticatedDefaultNotes().Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-011"]);
    }

    IEnumerable<SemanticId> AbsenceSpecifications() => _request.Model.Application.Modules.SelectMany(module => module.Features)
        .SelectMany(AllSlices).SelectMany(slice => slice.Specifications)
        .Where(specification => !specification.ThenAbsentReadModels.IsDefaultOrEmpty).Select(specification => specification.Id);

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
