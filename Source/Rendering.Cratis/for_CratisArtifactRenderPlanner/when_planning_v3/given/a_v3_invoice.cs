// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_v3.given;

public static class a_v3_invoice
{
    public static ExecutableSemanticModel Model(Func<SemanticSlice, SemanticSlice>? change = null, Func<SemanticApplication, SemanticApplication>? applicationChange = null)
    {
        var original = invoice_model.Compile(invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource));
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var application = original.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [change is null ? slice : change(slice)] }] }]
        };

        // Screenplay v3 requires at least one attachment. An unreferenced policy lets us
        // compare the same selected slice without pretending a body is executable.
        application = application with { Policies = [.. application.Policies, new SemanticPolicy("UnusedOpaquePolicy", new SemanticOpaquePolicyCondition("policy-body"))] };
        return ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, applicationChange is null ? application : applicationChange(application));
    }

    public static ArtifactRenderPlan Plan(ExecutableSemanticModel model) => invoice_model.Plan(model);
}
