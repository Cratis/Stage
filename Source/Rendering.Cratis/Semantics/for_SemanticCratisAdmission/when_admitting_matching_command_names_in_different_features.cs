// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_matching_command_names_in_different_features : a_register_project_render_request
{
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Because()
    {
        var context = new SemanticApplicationContext(_request);
        var original = context.Slice(_registerProject.Id);
        var otherFeature = original with { Path = [_module.Name, "OtherRegistration", _registerProject.Name] };
        var otherModule = original with { Path = ["OtherProjects", _feature.Name, _registerProject.Name] };
        var nestedFeature = original with { Path = [_module.Name, _feature.Name, "Nested", _registerProject.Name] };
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [original, otherFeature, otherModule, nestedFeature]);
    }

    [Fact] void should_allow_reusing_command_names_outside_the_feature() => _diagnostics.ShouldBeEmpty();
}
