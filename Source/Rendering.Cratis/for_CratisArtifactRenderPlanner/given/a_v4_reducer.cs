// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_v4_reducer : Specification
{
    protected ArtifactRenderRequest _request = null!;
    protected SemanticEventContract _event = null!;

    async Task Establish()
    {
        var source = when_rendering_a_pure_reducer.Source
            .Replace("event OrderPlaced\n        id Uuid\n        amount Decimal", "event OrderPlaced generation 1\n        id Uuid\n        oldAmount Decimal\n      event OrderPlaced generation 2\n        id Uuid\n        amount Decimal", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        _event = loaded.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single();
        _request = new(
            loaded.Model,
            SemanticExecutionPlan.Compile(loaded.Model).Plan!,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
    }
}
