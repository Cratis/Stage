// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_reducer_context_read_admission_is_missing : Specification
{
    SemanticApplicationContext _context = null!;
    Exception _error = null!;

    async Task Establish()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source);
        var request = new ArtifactRenderRequest(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        _context = new(request, new("Projects", "Projects"));
    }

    void Because()
    {
        var reducer = _context.Reducers.Single();
        _error = Catch.Exception(() => SemanticReducerArtifactRenderer.Render(_context.DeclaringSlice(reducer.ReadModel), reducer, _context));
    }

    [Fact] void should_refuse_rendering_with_a_typed_context_error() => _error.ShouldBeOfExactType<InvalidTypedContext>();
    [Fact] void should_name_the_missing_admission_information() => _error.Message.ShouldContain("lost its analyzed context reads after admission");
}
#endif
