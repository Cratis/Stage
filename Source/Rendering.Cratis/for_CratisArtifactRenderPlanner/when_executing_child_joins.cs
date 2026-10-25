// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The item page of Acme Commerce: an item lists its shelves as children keyed by the shelf code, and each shelf takes its label
/// from the event that registered the shelf, whichever came first.
/// </summary>
public class when_executing_child_joins : a_generated_application
{
    internal const string Source = """
        concept Sku : String
        concept LocationCode : String
        type ItemLocation
          locationCode LocationCode
          label String
          quantity Int
        module Stock
          feature Levels
            slice StateChange Registering
              command RegisterSku
                sku Sku identifier
                name String
                produces SkuRegistered
                  for sku
                  sku = sku
                  name = name
              event SkuRegistered
                sku Sku
                name String
              command RegisterLocation
                code LocationCode identifier
                label String
                produces LocationRegistered
                  for code
                  code = code
                  label = label
              event LocationRegistered
                code LocationCode
                label String
              command PlaceStock
                sku Sku identifier
                locationCode LocationCode
                quantity Int
                produces StockPlaced
                  for sku
                  locationCode = locationCode
                  quantity = quantity
              event StockPlaced
                locationCode LocationCode
                quantity Int
            slice StateView ItemPage
              readmodel ItemDetail
                sku Sku key
                name String
                locations ItemLocation[]
              query ItemBySku => ItemDetail optional
                by sku Sku
              projection ItemDetailProjection => ItemDetail
                no automap
                from SkuRegistered
                  sku = $eventSourceId
                  name = name
                children locations identified by locationCode
                  from StockPlaced key locationCode
                    parent $eventSourceId
                    quantity = quantity
                  join location on locationCode
                    with LocationRegistered
                      label = label
              specification TheShelfIsRegisteredFirst
                given SkuRegistered
                  for "ACM-1000"
                  sku = "ACM-1000"
                  name = "Acme mug"
                given LocationRegistered
                  for "A-01-01"
                  code = "A-01-01"
                  label = "Aisle A, shelf 1"
                given StockPlaced
                  for "ACM-1000"
                  locationCode = "A-01-01"
                  quantity = 20
                then readmodel ItemDetail
                  sku = "ACM-1000"
                  name = "Acme mug"
                  locations = [{"locationCode":"A-01-01","label":"Aisle A, shelf 1","quantity":20}]
              specification TheShelfIsRegisteredLast
                given SkuRegistered
                  for "ACM-2000"
                  sku = "ACM-2000"
                  name = "Acme cup"
                given StockPlaced
                  for "ACM-2000"
                  locationCode = "B-02-02"
                  quantity = 5
                given LocationRegistered
                  for "B-02-02"
                  code = "B-02-02"
                  label = "Aisle B, shelf 2"
                then readmodel ItemDetail
                  sku = "ACM-2000"
                  name = "Acme cup"
                  locations = [{"locationCode":"B-02-02","label":"Aisle B, shelf 2","quantity":5}]
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_label_each_shelf_of_the_item()
    {
        try
        {
            var build = await Run("child-join-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("child-join-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            results.ShouldHavePassedSpecifications();
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
