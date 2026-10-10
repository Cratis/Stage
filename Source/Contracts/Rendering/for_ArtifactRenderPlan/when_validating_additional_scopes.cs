// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_validating_additional_scopes : given.an_artifact_render_request
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => ArtifactRenderPlan.Create(_request with { AdditionalScopes = [_request.Scope] }, [], []));
    [Fact] void should_refuse_combining_application_with_another_scope() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
