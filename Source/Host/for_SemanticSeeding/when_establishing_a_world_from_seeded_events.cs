// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeding;

// A seeded event is no modeled command's product (the shop is told about a product by the catalog), so the world must take it as given.
public class when_establishing_a_world_from_seeded_events : Specification
{
    const string Model = """
        concept Money : Decimal

        module Shop
          feature Browse
            slice StateView Products
              readmodel ListedProduct
                sku   String key
                title String
                price Money
              query GetListedProduct => ListedProduct optional
                by sku String
              event ProductListed
                sku   String
                title String
                price Money
              projection ListedProductProjection => ListedProduct
                from ProductListed
                  sku   = sku
                  title = title
                  price = price
        """;

    SemanticWorld _world = null!;
    SemanticExecutionPlan _plan = null!;

    void Because()
    {
        _plan = compiled_plan.From(Model);
        var contract = _plan.Events.Values.Single(candidate => candidate.Name == "ProductListed");
        var fact = SemanticWorldRebuilder.SeedFact(_plan, contract, "ACM-1", new JsonObject { ["sku"] = "ACM-1", ["title"] = "Acme mug", ["price"] = 49 });
        _world = SemanticWorldRebuilder.Outcome(new SemanticEvaluator().EstablishWorld(_plan, ImmutableArray.Create(fact)));
    }

    [Fact] void should_hold_the_seeded_fact() => _world.Facts.Length.ShouldEqual(1);
    [Fact] void should_project_the_seeded_event_into_its_read_model() => _world.ReadModels.Length.ShouldEqual(1);
}
