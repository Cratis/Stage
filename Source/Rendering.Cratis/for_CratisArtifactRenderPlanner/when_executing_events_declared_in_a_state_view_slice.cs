// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A state-view slice may declare the events its projection reads or removes with, including an event no command produces and an event
/// another slice's command produces; they are generated with the slice.
/// </summary>
public class when_executing_events_declared_in_a_state_view_slice : a_generated_application
{
    internal const string Source = """
        concept Sku : String
        module Stock
          feature Levels
            slice StateChange Register
              command RegisterSku
                sku Sku identifier
                name String
                produces SkuRegistered
                  for sku
                  name = name
              event SkuRegistered
                name String
              command NoteDemand
                sku Sku identifier
                quantity Int
                produces DemandNoted
                  for sku
                  quantity = quantity
            slice StateView StockLevels
              event DemandNoted
                quantity Int
              event SkuRemoved
              readmodel StockLevel
                sku Sku key
                name String
                demand Int
              query AllStockLevels => observable StockLevel[]
              projection StockLevelProjection => StockLevel
                no automap
                from SkuRegistered
                  name = name
                  add demand by 0
                from DemandNoted
                  add demand by quantity
                remove with SkuRemoved
              specification DemandAccumulates
                given SkuRegistered
                  for "ACM-1000"
                  name = "Acme mug"
                given DemandNoted
                  for "ACM-1000"
                  quantity = 3
                then readmodel StockLevel
                  sku = "ACM-1000"
                  name = "Acme mug"
                  demand = 3
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_generate_the_declared_events_and_run_the_specifications()
    {
        try
        {
            var build = await Run("view-events-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("view-events-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
