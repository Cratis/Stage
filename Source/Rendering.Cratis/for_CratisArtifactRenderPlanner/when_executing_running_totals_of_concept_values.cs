// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A total adds an event value that is a concept (Quantity) into a plain number (a single
/// conversion, which Chronicle unwraps), with the zero starts beside it.
/// </summary>
public class when_executing_running_totals_of_concept_values : a_generated_application
{
    internal const string Source = """
        concept Sku : String
        concept Quantity : Int
          validate
            > 0 message "At least one"
        module Buying
          feature Demand
            slice StateChange RecordDemand
              command RecordDemand
                sku Sku identifier
                quantity Quantity
                produces DemandRecorded
                  for sku
                  sku = sku
                  quantity = quantity
              event DemandRecorded
                sku Sku
                quantity Quantity
            slice StateChange ReceiveGoods
              command ReceiveGoods
                sku Sku identifier
                quantity Quantity
                produces GoodsReceived
                  for sku
                  sku = sku
                  quantity = quantity
              event GoodsReceived
                sku Sku
                quantity Quantity
            slice StateView OpenDemand
              readmodel DemandRow
                sku Sku key
                demanded Int
                delivered Int
              query AllOpenDemand => observable DemandRow[]
              projection DemandRowProjection => DemandRow
                from DemandRecorded key sku
                  add demanded by quantity
                  add delivered by 0
                from GoodsReceived key sku
                  add demanded by 0
                  add delivered by quantity
              specification DemandAccumulatesPerSku
                given DemandRecorded
                  for "ACM-1000"
                  sku = "ACM-1000"
                  quantity = 3
                given DemandRecorded
                  for "ACM-1000"
                  sku = "ACM-1000"
                  quantity = 2
                then readmodel DemandRow
                  sku = "ACM-1000"
                  demanded = 5
                  delivered = 0
              specification GoodsBeforeAnyDemand
                given GoodsReceived
                  for "ACM-2000"
                  sku = "ACM-2000"
                  quantity = 4
                then readmodel DemandRow
                  sku = "ACM-2000"
                  demanded = 0
                  delivered = 4
              specification DemandThenGoods
                given DemandRecorded
                  for "ACM-3000"
                  sku = "ACM-3000"
                  quantity = 6
                given GoodsReceived
                  for "ACM-3000"
                  sku = "ACM-3000"
                  quantity = 2
                then readmodel DemandRow
                  sku = "ACM-3000"
                  demanded = 6
                  delivered = 2
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_add_concept_values_and_run_the_projected_specifications()
    {
        try
        {
            var build = await Run("concept-total-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("concept-total-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
