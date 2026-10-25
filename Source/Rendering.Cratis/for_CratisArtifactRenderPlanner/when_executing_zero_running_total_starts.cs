// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// 'add total by 0' only makes a required running total exist: the first event a row sees may be the one that adds to the other
/// total, and the reference seeds a missing total with 0. Chronicle's Add builder takes an event property only, so the renderer
/// emits nothing for the zero add and the projected row must still read 0 for it, with a payload key.
/// </summary>
public class when_executing_zero_running_total_starts : a_generated_application
{
    internal const string Source = """
        concept Sku : String
        module Buying
          feature Demand
            slice StateChange RecordDemand
              command RecordDemand
                sku Sku identifier
                quantity Int
                produces DemandRecorded
                  for sku
                  sku = sku
                  quantity = quantity
              event DemandRecorded
                sku Sku
                quantity Int
            slice StateChange ReceiveGoods
              command ReceiveGoods
                sku Sku identifier
                quantity Int
                produces GoodsReceived
                  for sku
                  sku = sku
                  quantity = quantity
              event GoodsReceived
                sku Sku
                quantity Int
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
    async Task should_render_only_the_real_additions_and_run_the_projected_specifications()
    {
        try
        {
            var projection = ReadGeneratedFile(GeneratedFiles("*.cs").Single(file => ReadGeneratedFile(file).Contains("Add(model => model.Demanded)", StringComparison.Ordinal)));
            Assert.Contains("Add(model => model.Demanded).With(evt => evt.Quantity)", projection, StringComparison.Ordinal);
            Assert.Contains("Add(model => model.Delivered).With(evt => evt.Quantity)", projection, StringComparison.Ordinal);
            Assert.DoesNotContain("by 0", projection, StringComparison.Ordinal);
            var build = await Run("zero-total-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("zero-total-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
