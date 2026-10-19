// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_inspecting_absence_keys : Specification
{
    SemanticSpecification _specification = null!;

    void Establish() => _specification = invoice_model.Compile(when_rendering_read_model_absence.Source).Application.Modules.Single()
        .Features.Single().Slices[0].Specifications.Single();

    [Fact] void should_admit_scalar_absence() => SemanticVersionFeatures.InSpecification(_specification).ShouldBeEmpty();
    [Fact] void should_refuse_non_scalar_absence_on_its_specification()
    {
        var specification = _specification with
        {
            ThenAbsentReadModels = [new(_specification.ThenAbsentReadModels[0].ReadModel, SemanticValue.Array([]))]
        };
        var feature = SemanticVersionFeatures.InSpecification(specification).Single();
        feature.Code.ShouldEqual("STAGE-ESM-027");
        feature.Artifact.ShouldEqual(specification.Id);
    }
}
