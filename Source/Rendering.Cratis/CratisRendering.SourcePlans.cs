// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    /// <summary>
    /// Compiles all supplied Screenplay sources and plans only the union of named scopes, never the scaffold.
    /// </summary>
    /// <param name="sources">Files and folders under a stable identity and attachment root.</param>
    /// <param name="selection">The exact authored module, feature, or slice paths.</param>
    /// <param name="options">Application names and optional domain placement.</param>
    /// <param name="cancellationToken">Cancellation of loading and planning.</param>
    /// <returns>The deterministic artifacts, output digest, or typed expected failures.</returns>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    /// <exception cref="InvalidArtifactRenderContract">A render contract is inconsistent.</exception>
    public static async Task<CratisPlanResult> PlanFrom(PlaySources sources, PlanSelection selection, CratisPlanOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var prepared = Prepare(options, scaffoldOnly: false);
        if (prepared.Diagnostic is not null) return CratisPlanResult.Create(options.ApplicationName, [], [prepared.Diagnostic]);
        var result = await SemanticModelLoader.LoadAsync(sources.Root, sources.Paths, sources.CatalogPath, options.ApplicationName, cancellationToken);
        if (!result.Success)
        {
            return CratisPlanResult.Create(options.ApplicationName, [], [.. result.Diagnostics.Select(diagnostic => new CratisPlanDiagnostic(diagnostic.Code, RenderSeverity(diagnostic.Severity), diagnostic.Message, default, diagnostic.Source))]);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var plan = PlanLoaded(result.Loaded!, selection, options, prepared.Profile!);
        cancellationToken.ThrowIfCancellationRequested();

        return plan;
    }

    /// <summary>
    /// Plans named scopes from an already loaded model without I/O or a scaffold.
    /// </summary>
    /// <param name="loaded">The compiled model, execution plan, and attachments.</param>
    /// <param name="selection">The union of exact authored paths.</param>
    /// <param name="options">Application names and optional domain placement.</param>
    /// <returns>The deterministic output plan or typed expected failures.</returns>
    /// <exception cref="InvalidArtifactRenderContract">A render contract is inconsistent.</exception>
    public static CratisPlanResult PlanFrom(LoadedSemanticModel loaded, PlanSelection selection, CratisPlanOptions options)
    {
        var prepared = Prepare(options, scaffoldOnly: false);

        return prepared.Diagnostic is not null
            ? CratisPlanResult.Create(options.ApplicationName, [], [prepared.Diagnostic], loaded.Model.Revision)
            : PlanLoaded(loaded, selection, options, prepared.Profile!);
    }

    /// <summary>
    /// Plans the application-root scaffold alone without any source model, Scene composition, or strings wiring.
    /// </summary>
    /// <param name="options">The application, project, and root namespace; Domain must be empty.</param>
    /// <returns>The scaffold artifacts and digest, with no semantic revision or underlying model plan.</returns>
    /// <exception cref="InvalidArtifactRenderContract">A package-owned scaffold input is inconsistent.</exception>
    public static CratisPlanResult PlanScaffold(CratisPlanOptions options)
    {
        var prepared = Prepare(options, scaffoldOnly: true);
        if (prepared.Diagnostic is not null) return CratisPlanResult.Create(options.ApplicationName, [], [prepared.Diagnostic]);
        var artifacts = prepared.Profile!.Inputs.Select(input =>
            CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact) ? artifact! :
                throw new InvalidArtifactRenderContract($"Package-owned scaffold input '{input.Name}' cannot be rendered."));

        return CratisPlanResult.Create(options.ApplicationName, [.. artifacts], []);
    }

    static CratisPlanResult PlanLoaded(LoadedSemanticModel loaded, PlanSelection selection, CratisPlanOptions options, ArtifactRenderProfile profile)
    {
        if (loaded.Model.Application.Name != options.ApplicationName)
        {
            return CratisPlanResult.Create(options.ApplicationName, [], [CratisPlanResult.Error("STAGE-PLAN-030", "The options' application name must match the loaded application.")], loaded.Model.Revision);
        }
        var resolved = PlanSelectionResolver.Resolve(loaded.Model.Application, selection);
        if (!resolved.Diagnostics.IsEmpty) return CratisPlanResult.Create(options.ApplicationName, [], resolved.Diagnostics, loaded.Model.Revision);
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, resolved.Scopes[0])
        {
            AdditionalScopes = resolved.Scopes.RemoveAt(0),
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            AttachmentDiagnostics = loaded.AttachmentDiagnostics,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, new(options.ProjectName, options.RootNamespace));
        if (context.SelectedSlices().Count == 0)
        {
            return CratisPlanResult.Create(options.ApplicationName, [], [CratisPlanResult.Error("STAGE-PLAN-014", "The selection matches no slices.")], loaded.Model.Revision);
        }
        var plan = new CratisArtifactRenderPlanner().Plan(request);

        var diagnostics = plan.Diagnostics.Select(diagnostic => new CratisPlanDiagnostic(diagnostic.Code, diagnostic.Severity, diagnostic.Message, diagnostic.Artifact, null)).ToImmutableArray();

        return CratisPlanResult.Create(plan.ApplicationName, plan.Artifacts, diagnostics, plan.SemanticRevision, plan);
    }

    static ArtifactRenderDiagnosticSeverity RenderSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => ArtifactRenderDiagnosticSeverity.Error,
        DiagnosticSeverity.Warning => ArtifactRenderDiagnosticSeverity.Warning,
        _ => ArtifactRenderDiagnosticSeverity.Information
    };

    static (ArtifactRenderProfile? Profile, CratisPlanDiagnostic? Diagnostic) Prepare(CratisPlanOptions options, bool scaffoldOnly)
    {
        if (scaffoldOnly && options.Domain.Length > 0)
        {
            return (null, CratisPlanResult.Error("STAGE-PLAN-022", "Scaffold-only planning does not accept a domain."));
        }
        if (!DomainPlacementInput.TryNormalize(options.Domain, out var domain))
        {
            return (null, CratisPlanResult.Error("STAGE-PLAN-020", "Domain must contain valid relative C# name segments, without traversal or empty segments."));
        }
        try
        {
            var profile = CreateProfile(options.ApplicationName, new(options.ProjectName, options.RootNamespace));
            if (DomainPlacementInput.IsReserved(domain, profile))
            {
                return (null, CratisPlanResult.Error("STAGE-PLAN-021", "Domain collides with an application-root scaffold or reserved folder."));
            }

            return (DomainPlacementInput.WithDomain(profile, domain), null);
        }
        catch (InvalidCratisBackendApplicationScaffold exception)
        {
            return (null, CratisPlanResult.Error("STAGE-PLAN-030", exception.Message));
        }
    }
}
