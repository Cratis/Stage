// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_checking_an_unhandled_type_reference : a_register_project_render_request
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => SemanticCratisAdmission.TypeExists(
        new SemanticApplicationContext(_request, _options),
        new((SemanticTypeReferenceKind)99, SemanticPrimitiveType.Unknown, default, false, false)));

    [Fact] void should_fail_instead_of_reporting_a_missing_type() => _error.ShouldBeOfExactType<UnsupportedSemanticRendering>();
}
