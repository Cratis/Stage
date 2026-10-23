// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class without_a_strings_catalog : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command(
        "return context.Value > 0;", message: " message $strings.validation.predicate")));

    [Fact] void should_refuse_without_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_missing_catalog_and_message_key() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message == "Validation 'Positive' message '$strings.validation.predicate' has no strings catalog. Supply the companion .strings files and default locale in the render profile.");
}
#endif
