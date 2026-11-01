// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_inventorying_allocated_responses;

internal static class response_inventory_model
{
    internal static readonly CratisRenderingOptions Options = new("InvoiceApp", "Invoices");

    internal const string Source = """
        module Orders
          feature Handling
            slice StateChange Place
              command PlaceOrder
                id String identifier
                title String
                produces OrderPlaced
                  for id
                  title = title
                returns
                  label = title
              event OrderPlaced
                title String
              specification PlacingAnOrder
                when PlaceOrder
                  id = "order-one"
                  title = "One"
                then OrderPlaced
                  title = "One"
                then returns
                  label = "One"
        """;

    internal static string SourceFor(string collision) => collision switch
    {
        "field" => Source.Replace("label", "placeOrderResponse", StringComparison.Ordinal),
        "sibling" => Source.Replace("      event OrderPlaced", "      command PlaceOrderResponse\n        title String\n        returns title\n      event OrderPlaced", StringComparison.Ordinal),
        "namespace" => Source + "\n    feature Place\n      slice StateChange PlaceOrderResponse\n        command InspectOrder\n          title String\n          returns title\n",
        _ => Source
    };

    internal static ArtifactRenderRequest Request(ExecutableSemanticModel model, ArtifactRenderScope? scope = null) => new(
        model,
        SemanticExecutionPlan.Compile(model).Plan!,
        CratisRendering.CreateProfile(model.Application.Name, Options),
        scope ?? new(ArtifactRenderScopeKind.Application, model.Application.Id));

    internal static SemanticApplicationContext Context(string collision) => new(Request(invoice_model.Compile(SourceFor(collision))), Options);

    internal static SemanticCommand Command(SemanticApplicationContext context) => context.Commands.Values.Single(command => command.Name == "PlaceOrder");
}
