// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication.given;

public class an_event_plan : a_policy_plan
{
    protected const string EventPath = "Events/Renamed.cs";

    protected ArtifactRenderPlan Events(string code, bool application = false, bool policies = false)
    {
        var scope = application ? _application.Scope : _plan.Scope;
        var request = new ArtifactRenderRequest(
            _loaded.Model,
            _loaded.Plan,
            CratisRendering.CreateProfile(_loaded.Model.Application.Name, new("InvoiceApp", "Invoices")),
            scope);

        return ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText(EventPath, code), .. policies ? _plan.Artifacts : []], []);
    }
}
#endif
