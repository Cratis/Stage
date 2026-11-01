// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Runtime.CompilerServices;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_inventorying_reducer_typed_contexts;

internal static class reducer_context_model
{
    internal static readonly CratisRenderingOptions Options = new("MinA3", "MinA3");

    internal static Task<LoadedSemanticModel> LoadMinA3([CallerFilePath] string path = "") =>
        SemanticModelLoader.LoadFromPathAsync(Path.Combine(Path.GetDirectoryName(path)!, "min-a3"), null, "MinA3");

    internal static ArtifactRenderRequest Request(LoadedSemanticModel loaded, ArtifactRenderScope? scope = null, string domain = "") =>
        new(
            loaded.Model,
            loaded.Plan,
            DomainPlacementInput.WithDomain(CratisRendering.CreateProfile(loaded.Model.Application.Name, Options), domain),
            scope ?? new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
}
#endif
