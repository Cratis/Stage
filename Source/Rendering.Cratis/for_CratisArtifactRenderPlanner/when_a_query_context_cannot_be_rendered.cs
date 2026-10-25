// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_query_context_cannot_be_rendered : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _loaded = opaque_policy_model.Load(
        """
        module People
          feature Search
            slice StateView FindPeople
              readmodel Person
                personId Uuid
                name String
              query FindPeople => Person[]
                filter name String
                performer
                  ```csharp
                  return readModels;
                  ```
        """ + a_render_with_an_invalid_typed_context.Independent) with
    {
        ImplementationContents = []
    };

    void Because() => _plan = opaque_policy_model.Plan(_loaded);

    [Fact] void should_preserve_the_existing_query_gap() => _plan.ImplementationGaps.Any(gap => gap.Role == global::Cratis.Screenplay.Semantics.SemanticImplementationRole.QueryPerformer).ShouldBeTrue();
    [Fact] void should_keep_independent_output() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Ping.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_query_contract() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => string.Equals(slice.Name, "FindPeople", StringComparison.Ordinal)).ReadModels.Single().Id)).ShouldBeTrue();
    [Fact] void should_be_partial() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_compile_the_remaining_output() => opaque_policy_model.Compile(_plan).GetTypes().Any(type => string.Equals(type.Name, "Ping", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
