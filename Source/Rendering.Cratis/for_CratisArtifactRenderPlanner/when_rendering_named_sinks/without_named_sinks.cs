// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks;

public class without_named_sinks : Specification
{
    SemanticCompilation _compilation = null!;
    ArtifactRenderProfile _profile = null!;
    ArtifactRenderProfile _withSinks = null!;

    void Establish()
    {
        _compilation = a_model_with_named_sinks.Compile(a_model_with_named_sinks.WithoutSinks);
        _profile = CratisRendering.WithPassiveReadModels(CratisRendering.WithRealization(CratisRendering.CreateProfile(_compilation.Model.Application.Name, new("Organization", "Organization")), Contracts.Semantics.ApplicationRealization.From(_compilation)), _compilation);
    }

    void Because() => _withSinks = CratisRendering.WithSinks(_profile, _compilation);

    [Fact] void should_keep_the_profile_inputs() => Assert.Equal(_profile.Inputs, _withSinks.Inputs);
    [Fact] void should_keep_the_render_input_hash() => a_model_with_named_sinks.InputDigest(_withSinks).ShouldEqual(a_model_with_named_sinks.InputDigest(_profile));
    [Fact] void should_realize_no_sinks() => Contracts.Semantics.ApplicationRealization.From(_compilation).Sinks.IsEmpty.ShouldBeTrue();
}
#endif
