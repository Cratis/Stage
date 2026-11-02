// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Realization;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

internal static class CratisScaffoldArtifacts
{
    internal static IEnumerable<PlannedArtifact> Render(ArtifactRenderProfile profile, string rootNamespace, ImmutableArray<SemanticId> sources, string? applicationName = null)
    {
        var realization = applicationName is null ? null : ApplicationRealizationInput.From(profile);
        var strings = StringsCatalogInput.From(profile);
        var sceneName = $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}";
        foreach (var input in profile.Inputs.Where(input => input.Name != sceneName && !CratisScaffoldProtocolInputs.Contains(input.Name)))
        {
            if (!CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact))
            {
                throw new InvalidArtifactRenderContract($"Package-owned scaffold input '{input.Name}' cannot be rendered.");
            }

            var planned = artifact!.RelativePath == "Program.cs" && strings is not null
                ? PlannedArtifact.CreateText(artifact.RelativePath, StringsCatalogInput.ConfigureProgram(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), strings, rootNamespace))
                : artifact;
            yield return realization is null ? planned : RealizationScaffold.Patch(planned, realization, rootNamespace);
        }

        if (realization is not null)
        {
            foreach (var planned in RealizationArtifacts.Render(realization, applicationName!, rootNamespace, strings?.DefaultLocale ?? RealizationArtifacts.DefaultLanguage, sources))
            {
                yield return planned;
            }
        }

        if (profile.Inputs.SingleOrDefault(input => input.Name == ReducerConventionInput.Name) is { } convention)
        {
            yield return PlannedArtifact.CreateText(ReducerConventionInput.RelativePath, Encoding.UTF8.GetString(convention.Bytes.AsSpan()), sources);
        }

        if (strings is not null)
        {
            yield return PlannedArtifact.CreateText(StringsCatalogInput.RelativePath, StringsCatalogInput.Render(strings, rootNamespace), sources);
        }
    }
}
