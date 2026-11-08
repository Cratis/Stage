// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_warning_on_secret_read_model_properties.given;

public class a_secret_read_model_plan : Specification
{
    protected const string Code = "STAGE-ESM-069";

    protected ArtifactRenderPlan _plan = null!;

    protected static ExecutableSemanticModel Compile(bool marked = true) =>
        invoice_model.Compile((marked ? a_concept_compliance_model.Source : a_concept_compliance_model.Unmarked(a_concept_compliance_model.Source)) + "\n" + a_concept_compliance_model.Lookup);

    protected static ExecutableSemanticModel WithoutPopulation(ExecutableSemanticModel model)
    {
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with { Projections = [], Reducers = [] })]
                })]
            })]
        };

        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
    }

    protected IEnumerable<ArtifactRenderDiagnostic> Warnings => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == Code);
}
