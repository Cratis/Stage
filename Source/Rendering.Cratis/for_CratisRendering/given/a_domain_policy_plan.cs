// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;

public class a_domain_policy_plan : Specification
{
    static readonly Lazy<LoadedSemanticModel> _model = new(() => opaque_policy_model.Load(
        opaque_policy_model.Source("return context.Identity.IsAuthenticated && context.Subject != \"\";", "Custom and Staff") + "\n" +
        when_rendering_a_pure_reducer.Source
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("command PlaceOrder\n", "command PlaceOrder\n        authorize Staff\n", StringComparison.Ordinal)
            .Replace(
                "      event OrderPlaced",
                """
                      specification DeniedOrder
                        given caller
                        when PlaceOrder
                          id = "00000000-0000-0000-0000-000000000001"
                          amount = 10
                        then denied
                      event OrderPlaced
                """,
                StringComparison.Ordinal)));

    protected LoadedSemanticModel _loaded = null!;
    protected CratisPlanOptions _options = new("InvoiceModel", "InvoiceApp", "Invoices");
    protected PlanSelection _selection = new([PlanSelectionEntry.Module("Billing"), PlanSelectionEntry.Module("Orders")]);

    void Establish() => _loaded = _model.Value;

    protected CratisPlanResult Plan(string domain) => CratisRendering.PlanFrom(_loaded, _selection, _options with { Domain = domain });
}
#endif
