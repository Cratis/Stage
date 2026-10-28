// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Rendering.Model;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Plans artifacts by interpreting a <c lang="text">stage-target/v1</c> kit through the <c lang="text">stage-templates/v1</c> profile over the render model.
/// </summary>
/// <remarks>This planner is prerelease and curated-only: no host wires it in yet.</remarks>
/// <param name="languages">The registered languages a kit can bind to.</param>
/// <param name="options">The planner limits, or <see langword="null"/> for the profile defaults.</param>
public sealed class DeclarativeArtifactRenderPlanner(IEnumerable<IDeclarativeLanguage> languages, DeclarativePlannerOptions? options = null) : IArtifactRenderPlanner
{
    /// <summary>
    /// The renderer identity a profile must name.
    /// </summary>
    public const string Renderer = "stage-declarative";

    /// <summary>
    /// The renderer version a profile must name.
    /// </summary>
    public const string RendererVersion = "1.0.0-prerelease";

    readonly ImmutableArray<IDeclarativeLanguage> _languages = [.. languages];
    readonly DeclarativePlannerOptions _options = options ?? new();

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(ArtifactRenderRequest request)
    {
        var admission = DeclarativeAdmission.Admit(request.Profile, _languages, _options);
        if (!admission.Admitted)
        {
            return Refuse(request, admission.Diagnostics);
        }

        var model = BuildModel(request, out var failure);
        if (failure is not null)
        {
            return Refuse(request, [failure]);
        }

        using (model)
        using (var session = admission.Templates!.BeginPlan(_options.Budget ?? Templates.TemplateBudget.Default, admission.Language!))
        {
            var expansion = DeclarativeOutputExpansion.Expand(session, admission.Kit!.Manifest, model!.RootElement);
            if (expansion.Refusal is not null)
            {
                return Refuse(request, [expansion.Refusal]);
            }

            var rendering = DeclarativeRendering.Render(session, admission.Language!, expansion.Outputs, model.RootElement, request);
            return rendering.Refusal is not null
                ? Refuse(request, [rendering.Refusal])
                : Assemble(request, rendering.Files, admission.Components);
        }
    }

    // Kit and authored input must never surface as an exception: a contract violation while building artifacts is a refusal.
    internal static ArtifactRenderPlan Assemble(ArtifactRenderRequest request, ImmutableArray<DeclarativeRenderedFile> files, ImmutableArray<ArtifactRenderComponent> components)
    {
        try
        {
            return DeclarativePlanAssembly.Assemble(request, files, components);
        }
        catch (InvalidArtifactRenderContract error)
        {
            return Refuse(request, [DeclarativeOutputExpansion.Refuse(DeclarativeCodes.OutputText, error.Message)]);
        }
    }

    internal static JsonDocument? BuildModel(ArtifactRenderRequest request, out ArtifactRenderDiagnostic? failure)
    {
        failure = null;
        try
        {
            return JsonDocument.Parse(RenderModelJson.Serialize(RenderModelBuilder.Build(request)));
        }
        catch (Exception error) when (error is InvalidArtifactRenderContract or InvalidRenderModel)
        {
            failure = new(DeclarativeCodes.RenderModelInvalid, ArtifactRenderDiagnosticSeverity.Error, error.Message, default);
            return null;
        }
    }

    static ArtifactRenderPlan Refuse(ArtifactRenderRequest request, ImmutableArray<ArtifactRenderDiagnostic> errors) => ArtifactRenderPlan.Create(request, [], errors);
}
