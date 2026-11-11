// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Realization;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Constraints;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs;
using Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

namespace Cratis.Stage.Rendering.Cratis;

public sealed partial class CratisArtifactRenderPlanner
{
    // The application's shared UI: the Scene composition, the module binding it to the generated Arc proxies, and the
    // document and module the emitted Stage runtime renders it from. An application plan composes it over every slice;
    // a scoped plan carrying the user interface input composes it over exactly the slices whose proxies are present,
    // so the bindings never import a proxy that has not been generated.
    static List<PlannedArtifact> UserInterfaceArtifacts(
        ArtifactRenderRequest request,
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> userInterface,
        string? sceneJson)
    {
        // A caller that authored screens carries its own composition in the profile. A caller that did not
        // gets one composed from the model here, so a generated application is usable without a hand-written
        // screen. Either way the binding module accompanies it, because it is what the payload refers to.
        var documentJson = sceneJson;
        if (!SceneCompositionInput.IsCarriedBy(request.Profile) && DefaultSceneComposition.Create(context, userInterface) is { } composed)
        {
            documentJson = CanonicalSceneJson.Serialize(composed);
        }

        if (documentJson is null) return [];

        return
        [
            PlannedArtifact.CreateText(SceneCompositionInput.RelativePath, documentJson, [context.Application.Id]),
            PlannedArtifact.CreateText(SceneBindingsRenderer.RelativePath, SceneBindingsRenderer.Render(context, userInterface), [context.Application.Id]),
            PlannedArtifact.CreateText(StageSceneDocument.RelativePath, StageSceneDocument.Render(documentJson, context), [context.Application.Id]),
            PlannedArtifact.CreateText(StageModuleRenderer.RelativePath, StageModuleRenderer.Render(context, userInterface), [context.Application.Id])
        ];
    }

    // Application plans carry the UI of every slice. A scoped plan carries shared UI only when the caller names the
    // slices whose UI is present; the plan's own slices are always among them, because this plan generates them.
    static IReadOnlyList<LocatedSemanticSlice>? UserInterfaceSlices(
        ArtifactRenderRequest request,
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> selected,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (request.Scope.Kind == ArtifactRenderScopeKind.Application) return context.AllSlices;
        if (!UserInterfaceInput.TryRead(request.Profile, out var present) || present is not { } named) return null;

        var known = context.AllSlices.Select(slice => slice.Slice.Id.ToString()).ToHashSet(StringComparer.Ordinal);
        foreach (var unknown in named.Where(identity => !known.Contains(identity)))
        {
            diagnostics.Add(Error("STAGE-SCREEN-007", $"The user interface input names '{unknown}', which is not a slice of this model.", context.Application.Id));
        }

        var elsewhere = DomainPlacements(request, context, known, diagnostics);

        // A slice this plan generates is placed under the plan's own domain, wherever it was generated before.
        var own = selected.Select(slice => slice.Slice.Id.ToString()).ToHashSet(StringComparer.Ordinal);
        var included = named.Concat(own).Concat(elsewhere.Keys).ToHashSet(StringComparer.Ordinal);
        return [.. context.AllSlices
            .Where(slice => included.Contains(slice.Slice.Id.ToString()))
            .Select(slice => PlacedSlice(context, slice, own, elsewhere))];
    }

    static Dictionary<string, string> DomainPlacements(ArtifactRenderRequest request, SemanticApplicationContext context, HashSet<string> known, List<ArtifactRenderDiagnostic> diagnostics)
    {
        UserInterfaceDomainsInput.TryRead(request.Profile, out var domains);
        var elsewhere = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (domain, identities) in domains?.AsEnumerable() ?? [])
        {
            foreach (var identity in identities)
            {
                if (!known.Contains(identity))
                {
                    diagnostics.Add(Error("STAGE-SCREEN-007", $"The user interface domains input names '{identity}', which is not a slice of this model.", context.Application.Id));
                }

                elsewhere[identity] = domain;
            }
        }

        return elsewhere;
    }

    static LocatedSemanticSlice PlacedSlice(SemanticApplicationContext context, LocatedSemanticSlice slice, HashSet<string> own, Dictionary<string, string> elsewhere) =>
        !own.Contains(slice.Slice.Id.ToString()) && elsewhere.TryGetValue(slice.Slice.Id.ToString(), out var domain)
            ? slice with { Path = [.. domain.Split('/', StringSplitOptions.RemoveEmptyEntries), .. slice.Path.Skip(context.Domain.Count)] }
            : slice;
}
