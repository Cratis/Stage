// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The basket of Acme Commerce: a line adds what it is worth (<c language="csharp">unitPrice * quantity</c>) to the basket, a removed line takes
/// it off again. The generated application builds and its specifications pass in Chronicle's projection engine, which adds the
/// member the event carries for the product.
/// </summary>
public class when_executing_a_product_total : a_generated_application
{
    internal const string Source = """
        concept BasketId : String
        concept Money : Decimal
          validate
            >= 0  message "An amount cannot be negative"
            decimals <= 2  message "An amount has at most two decimals"
        concept Quantity : Int
          validate
            > 0  message "A quantity is at least one"
        module Shop
          feature Basket
            slice StateChange Lines
              command AddLine
                basketId  BasketId identifier
                quantity  Quantity
                unitPrice Money
                produces LineAdded
                  for basketId
                  quantity  = quantity
                  unitPrice = unitPrice
              event LineAdded
                quantity  Quantity
                unitPrice Money
              command RemoveLine
                basketId  BasketId identifier
                quantity  Quantity
                unitPrice Money
                produces LineRemoved
                  for basketId
                  quantity  = quantity
                  unitPrice = unitPrice
              event LineRemoved
                quantity  Quantity
                unitPrice Money
            slice StateView BasketTotal
              readmodel BasketSize
                id         BasketId key
                lineCount  Int
                addedValue Money optional
              query GetBasketSize => BasketSize optional
                by id BasketId
              projection BasketSizeProjection => BasketSize
                from LineAdded
                  increment lineCount
                  add addedValue by unitPrice * quantity
                from LineRemoved
                  decrement lineCount
                  subtract addedValue by unitPrice * quantity
              specification TwoLinesAreWorthTheirProducts
                given LineAdded
                  for "b1"
                  quantity  = 3
                  unitPrice = 19.5
                given LineAdded
                  for "b1"
                  quantity  = 2
                  unitPrice = 10
                then readmodel BasketSize
                  id         = "b1"
                  lineCount  = 2
                  addedValue = 78.5
              specification ARemovedLineTakesItsProductOff
                given LineAdded
                  for "b2"
                  quantity  = 3
                  unitPrice = 19.5
                given LineAdded
                  for "b2"
                  quantity  = 2
                  unitPrice = 10
                given LineRemoved
                  for "b2"
                  quantity  = 2
                  unitPrice = 10
                then readmodel BasketSize
                  id         = "b2"
                  lineCount  = 1
                  addedValue = 58.5
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_add_and_subtract_the_product_of_the_event()
    {
        try
        {
            var build = await Run("product-total-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("product-total-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            results.ShouldHavePassedSpecifications();
            Assert.DoesNotContain("Failed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
