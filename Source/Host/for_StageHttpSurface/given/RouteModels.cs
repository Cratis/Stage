// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Projections;

namespace Cratis.Stage.Host.for_StageHttpSurface.given;

internal static class RouteModels
{
    internal const string SameNamedCommands = """
        module Orders
          feature Checkout
            slice StateChange PlaceOrder
              command DoIt
                orderId Uuid identifier
                produces ItHappened
                  orderId = orderId
              event ItHappened
                orderId Uuid
            slice StateChange CancelOrder
              command DoIt
                orderId Uuid identifier
                produces ItHappened
                  orderId = orderId
              event ItHappened
                orderId Uuid
        """;

    internal static Slice Command(string slice, string name = "DoIt") => new(
        Guid.NewGuid(),
        slice,
        SliceType.StateChange,
        [],
        new CommandDefinition(Guid.NewGuid(), name, string.Empty, string.Empty, [], string.Empty, [new ProducedEvent("ItHappened", null, [], [])], "orderId"),
        null,
        []);

    internal static Slice Query(string slice, string name = "Order") => new(
        Guid.NewGuid(),
        slice,
        SliceType.StateView,
        [],
        null,
        new ReadModelDefinition(Guid.NewGuid(), name, "{}", null),
        []);

    internal static Feature Feature(string name, Slice[] slices, params Feature[] children) =>
        new(Guid.NewGuid(), name, null, children, slices);

    internal static EventModel Model(params Slice[] slices) => WithFeatures(Feature("Checkout", slices));

    internal static EventModel WithFeatures(params Feature[] features) => new(
        Guid.NewGuid(),
        "Routing",
        [new ModuleCollection(Guid.NewGuid(), Guid.Empty, [new Module(Guid.NewGuid(), Guid.Empty, Guid.Empty, "Orders", features)])]);

    internal static EventModel Reverse(EventModel model) => model with
    {
        Collections = [.. model.Collections.Reverse().Select(collection => collection with
        {
            Modules = [.. collection.Modules.Reverse().Select(module => module with
            {
                Features = [.. module.Features.Reverse().Select(ReverseFeature)]
            })]
        })]
    };

    static Feature ReverseFeature(Feature feature) => feature with
    {
        Slices = [.. feature.Slices.Reverse()],
        SubFeatures = [.. feature.SubFeatures.Reverse().Select(ReverseFeature)]
    };
}
