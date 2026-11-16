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
    internal static IEnumerable<PlannedArtifact> Render(ArtifactRenderProfile profile, string rootNamespace, ImmutableArray<SemanticId> sources, string? applicationName = null, bool generatedWorkflows = false, string? contractsFolder = null, bool usesGraphs = false)
    {
        var context = new ScaffoldContext(profile, rootNamespace, sources, applicationName, generatedWorkflows, contractsFolder, usesGraphs);
        return RenderInputs(context).Concat(RenderRealization(context)).Concat(RenderModelAssets(context)).Concat(RenderAttachments(context));
    }

    static IEnumerable<PlannedArtifact> RenderInputs(ScaffoldContext context)
    {
        var sceneName = $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}";
        foreach (var input in context.Profile.Inputs.Where(input => input.Name != sceneName && !CratisScaffoldProtocolInputs.Contains(input.Name)))
        {
            if (!CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact))
            {
                throw new InvalidArtifactRenderContract($"Package-owned scaffold input '{input.Name}' cannot be rendered.");
            }
            yield return Patch(ConfigureProgram(artifact!, context), context);
        }
    }

    static PlannedArtifact ConfigureProgram(PlannedArtifact artifact, ScaffoldContext context)
    {
        if (artifact.RelativePath != "Program.cs") return artifact;
        var planned = context.Strings is null ? artifact : PlannedArtifact.CreateText(artifact.RelativePath, StringsCatalogInput.ConfigureProgram(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), context.Strings, context.RootNamespace));
        return context.GeneratedWorkflows
            ? PlannedArtifact.CreateText(planned.RelativePath, Encoding.UTF8.GetString(planned.Bytes.AsSpan()).Replace("builder.Services.AddEventSourcedWorkflows();", $"builder.Services.AddEventSourcedWorkflows({context.RootNamespace}.Lifecycles.GeneratedWorkflows.Configure);", StringComparison.Ordinal), planned.Sources)
            : planned;
    }

    static PlannedArtifact Patch(PlannedArtifact planned, ScaffoldContext context)
    {
        planned = context.Realization is null ? planned : RealizationScaffold.Patch(planned, context.Realization, context.RootNamespace, ForwardedIdentitySigning.Settings(context.ApplicationName!, context.Signed));
        planned = context.ContractsFolder is null ? planned : EventContractsScaffold.Patch(planned, context.ContractsFolder);
        planned = context.UsesGraphs && context.ApplicationName is not null ? GraphScaffold.Patch(planned, context.ApplicationName) : planned;
        return SinksScaffold.Patch(planned, context.Sinks);
    }

    static IEnumerable<PlannedArtifact> RenderRealization(ScaffoldContext context) => context.Realization is null ? [] :
        RealizationArtifacts.Render(context.Realization, context.ApplicationName!, context.RootNamespace, context.Strings?.DefaultLocale ?? RealizationArtifacts.DefaultLanguage, context.Sources, context.Signed);

    static IEnumerable<PlannedArtifact> RenderModelAssets(ScaffoldContext context) => context.ApplicationName is null ? [] :
        (ModelAssetsInput.From(context.Profile) ?? Contracts.Semantics.ModelAssets.None).Files
            .Select(file => PlannedArtifact.CreateBinary($".frontend/public/assets/{file.Key}", file.Value, context.Sources));

    static IEnumerable<PlannedArtifact> RenderAttachments(ScaffoldContext context)
    {
        if (context.ContractsFolder is not null)
        {
            yield return EventContractsScaffold.Project(context.ContractsFolder, context.ApplicationName!, context.Sources);
        }
        if (context.Profile.Inputs.SingleOrDefault(input => input.Name == ReducerConventionInput.Name) is { } convention)
        {
            yield return PlannedArtifact.CreateText(ReducerConventionInput.RelativePath, Encoding.UTF8.GetString(convention.Bytes.AsSpan()), context.Sources);
        }
        if (context.Strings is not null)
        {
            yield return PlannedArtifact.CreateText(StringsCatalogInput.RelativePath, StringsCatalogInput.Render(context.Strings, context.RootNamespace), context.Sources);
        }
    }

    sealed record ScaffoldContext(ArtifactRenderProfile Profile, string RootNamespace, ImmutableArray<SemanticId> Sources, string? ApplicationName, bool GeneratedWorkflows, string? ContractsFolder, bool UsesGraphs)
    {
        internal Contracts.Semantics.ApplicationRealization? Realization { get; } = ApplicationName is null ? null : ApplicationRealizationInput.From(Profile);
        internal bool Signed { get; } = SignedForwardedIdentityInput.IsCarriedBy(Profile);
        internal StringsCatalogInput.Catalog? Strings { get; } = StringsCatalogInput.From(Profile);
        internal Contracts.Semantics.RealizedSinks Sinks { get; } = ApplicationName is not null && SinksInput.TryRead(Profile, null, out var declared) ? declared : Contracts.Semantics.RealizedSinks.None;
    }
}
