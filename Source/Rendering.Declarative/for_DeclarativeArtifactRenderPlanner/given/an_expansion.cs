// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.given;

/// <summary>
/// Expands the outputs of a manifest variant over a request.
/// </summary>
public class an_expansion : a_declarative_planner
{
    private protected static DeclarativeOutputExpansion Expand(ArtifactRenderRequest request, string manifest = Manifest, TemplateBudget? budget = null)
    {
        var admission = DeclarativeAdmission.Admit(Profile(Archive(KitFiles(manifest))), [new a_test_language()], new());
        using var model = DeclarativeArtifactRenderPlanner.BuildModel(request, out _);
        using var session = admission.Templates!.BeginPlan(budget ?? TemplateBudget.Default, admission.Language!);
        return DeclarativeOutputExpansion.Expand(session, admission.Kit!.Manifest, model!.RootElement);
    }

    private protected static string[] Names(DeclarativeOutputExpansion expansion) => [.. expansion.Outputs.Select(_ => _.Output.Id + ":" + _.ItemId)];

    private protected static ArtifactRenderRequest Scoped(ArtifactRenderRequest request, ArtifactRenderScopeKind kind, Cratis.Screenplay.Semantics.SemanticId id) => request with { Scope = new(kind, id) };
}
