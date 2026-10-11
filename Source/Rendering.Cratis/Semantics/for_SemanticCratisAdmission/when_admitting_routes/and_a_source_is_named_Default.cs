// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_a_source_is_named_Default : a_routed_command
{
    Exception _failure = null!;

    void Establish() => _source = _source with { SourceKind = "Default" };
    void Because() => _failure = Catch.Exception(ReplaceSlice);

    // Screenplay rejects this sentinel while constructing the ESM, before Stage can receive it.
    [Fact] void should_refuse_the_chronicle_sentinel_before_planning() => _failure.ShouldBeOfExactType<InvalidSemanticContract>();
}
