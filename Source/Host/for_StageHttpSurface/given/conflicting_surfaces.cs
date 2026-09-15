// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;

namespace Cratis.Stage.Host.for_StageHttpSurface.given;

public class conflicting_surfaces : Specification
{
    protected readonly Dictionary<string, EventModel> _models = [];

    void Establish()
    {
        _models["case"] = RouteModels.Model(RouteModels.Command("Place"), RouteModels.Command("place"));
        _models["kebab"] = RouteModels.Model(RouteModels.Command("PlaceOrder"), RouteModels.Command("placeOrder"));
        _models["sanitization"] = RouteModels.Model(RouteModels.Command("Place-Order"), RouteModels.Command("PlaceOrder"));
        _models["empty identifier"] = RouteModels.Model(RouteModels.Command("!!!"), RouteModels.Command("???"));
        _models["query normalization"] = RouteModels.Model(RouteModels.Query("Report"), RouteModels.Query("report"));

        var collection = RouteModels.Model(RouteModels.Command("PlaceOrder"));
        _models["omitted collection"] = collection with { Collections = [collection.Collections[0], collection.Collections[0] with { Id = Guid.NewGuid() }] };
        var command = RouteModels.Command("PlaceOrder", "Order");
        _models["shared type cache"] = RouteModels.Model(command with { ReadModel = RouteModels.Query("Unused").ReadModel });

        _models["execute versus validate"] = RouteModels.WithFeatures(RouteModels.Feature(
            "Checkout",
            [RouteModels.Command("PlaceOrder")],
            RouteModels.Feature("PlaceOrder", [RouteModels.Command("DoIt", "Validate")])));
        _models["canonical versus singleton alias"] = RouteModels.WithFeatures(RouteModels.Feature(
            "Checkout",
            [RouteModels.Command("PlaceOrder")],
            RouteModels.Feature("PlaceOrder", [], RouteModels.Feature("DoIt", [RouteModels.Command("Other", "Other")]))));
        _models["canonical versus ambiguous alias"] = RouteModels.WithFeatures(RouteModels.Feature(
            "Checkout",
            [RouteModels.Command("PlaceOrder")],
            RouteModels.Feature("PlaceOrder", [RouteModels.Command("Other"), RouteModels.Command("Another")])));
        _models["query cross depth alias"] = RouteModels.WithFeatures(RouteModels.Feature(
            "Checkout",
            [RouteModels.Query("Report")],
            RouteModels.Feature("Report", [RouteModels.Query("AnotherReport")])));
    }
}
