// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

using static Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.ThreeWayOutcomes;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Compares add and subtract by a product of two event values (ESM v29), which the renderer lowers to a reducer, with the
/// reference evaluator: exact decimal totals, half-to-even rounding of a whole total, a model created by a subtraction, and a
/// model recreated after removal. Stage's executor does not run projection arithmetic yet.
/// </summary>
public class when_comparing_projection_products(when_comparing_projection_products.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_projection_products.context>
{
    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(4);
    [Fact] void should_match_every_executed_path() => Report(fixture, output);
    [Fact] void should_render_and_pass_every_specification() => Assert.All(fixture.Outcomes, outcome => Assert.Contains("Reference=Passed, Stage=Unsupported(Projection), Rendered=Passed", outcome, StringComparison.Ordinal));

    public class context : a_three_way_application
    {
        const string Basket = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        const string Source = $$"""
            concept BasketId : Uuid
            concept Money : Decimal
            module Shop
              feature Baskets
                slice StateChange AddLine
                  command AddLine
                    basketId BasketId identifier
                    unitPrice Money
                    quantity Int
                    produces LineAdded
                      for basketId
                      unitPrice = unitPrice
                      quantity = quantity
                  event LineAdded
                    unitPrice Money
                    quantity Int
                  specification AddingASecondLine
                    given LineAdded
                      for "{{Basket}}"
                      unitPrice = 2.5
                      quantity = 1
                    when AddLine
                      basketId = "{{Basket}}"
                      unitPrice = 3.5
                      quantity = 1
                    then LineAdded
                      unitPrice = 3.5
                      quantity = 1
                    then readmodel Basket
                      basketId = "{{Basket}}"
                      total = 6.0
                      units = 2
                      rounded = 6
                  specification AddingAfterClearing
                    given LineAdded
                      for "{{Basket}}"
                      unitPrice = 1
                      quantity = 2
                    given BasketCleared
                      for "{{Basket}}"
                    when AddLine
                      basketId = "{{Basket}}"
                      unitPrice = 1.5
                      quantity = 3
                    then LineAdded
                      unitPrice = 1.5
                      quantity = 3
                    then readmodel Basket
                      basketId = "{{Basket}}"
                      total = 4.5
                      units = 3
                      rounded = 4
                slice StateChange RemoveLine
                  command RemoveLine
                    basketId BasketId identifier
                    unitPrice Money
                    quantity Int
                    produces LineRemoved
                      for basketId
                      unitPrice = unitPrice
                      quantity = quantity
                  event LineRemoved
                    unitPrice Money
                    quantity Int
                  specification RemovingPartOfALine
                    given LineAdded
                      for "{{Basket}}"
                      unitPrice = 10.25
                      quantity = 3
                    when RemoveLine
                      basketId = "{{Basket}}"
                      unitPrice = 10.25
                      quantity = 1
                    then LineRemoved
                      unitPrice = 10.25
                      quantity = 1
                    then readmodel Basket
                      basketId = "{{Basket}}"
                      total = 20.50
                      units = 2
                      rounded = 21
                  specification RemovingFromAnEmptyBasket
                    when RemoveLine
                      basketId = "{{Basket}}"
                      unitPrice = 0.5
                      quantity = 5
                    then LineRemoved
                      unitPrice = 0.5
                      quantity = 5
                    then readmodel Basket
                      basketId = "{{Basket}}"
                      total = -2.5
                      units = -5
                      rounded = -2
                slice StateChange ClearBasket
                  command ClearBasket
                    basketId BasketId identifier
                    produces BasketCleared
                      for basketId
                  event BasketCleared
                slice StateView BasketLookup
                  readmodel Basket
                    basketId BasketId key
                    total Money?
                    units Int
                    rounded Int?
                  query BasketById => Basket optional
                    by basketId BasketId
                  projection BasketProjection => Basket
                    from LineAdded
                      add total by unitPrice * quantity
                      add units by quantity
                      add rounded by unitPrice * quantity
                    from LineRemoved
                      subtract total by unitPrice * quantity
                      subtract units by quantity
                      subtract rounded by unitPrice * quantity
                    remove with BasketCleared
            """;

        protected override string ApplicationName => "Shop";
        protected override string ProjectFile => "BackendHost.csproj";
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("products", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("products"), "products", "Products.play", Source);
            var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
