// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Renders running totals and clears as model-bound attributes for new applications, and pins the bytes of the classic and version three conventions.
/// </summary>
public class when_rendering_model_bound_running_totals
{
    internal const string Source = """
        concept Sku : String
        concept Quantity : Int
          validate
            > 0 message "At least one"
        concept StockQuantity : Int
          validate
            >= 0 message "Not negative"
        module Warehouse
          feature Overview
            slice StateChange Stock
              command RegisterSku
                sku Sku identifier
                produces SkuRegistered
                  for sku
                  sku = sku
              event SkuRegistered
                sku Sku
              command ReceiveStock
                sku Sku identifier
                quantity Quantity
                batch String
                produces StockReceived
                  for sku
                  sku = sku
                  quantity = quantity
                  batch = batch
              event StockReceived
                sku Sku
                quantity Quantity
                batch String
              command DispatchStock
                sku Sku identifier
                quantity Quantity
                produces StockDispatched
                  for sku
                  sku = sku
                  quantity = quantity
              event StockDispatched
                sku Sku
                quantity Quantity
              command ResetShelf
                sku Sku identifier
                produces ShelfReset
                  for sku
                  sku = sku
              event ShelfReset
                sku Sku
            slice StateView Shelf
              readmodel ShelfFigures
                sku Sku key
                receipts Int
                unitsOnHand StockQuantity optional
                lastBatch String optional
                parcelsOpen Int
              query AllShelves => observable ShelfFigures[]
              projection ShelfFiguresProjection => ShelfFigures
                from SkuRegistered key sku
                  sku = sku
                from StockReceived key sku
                  increment receipts
                  add unitsOnHand by quantity
                  lastBatch = batch
                from StockDispatched key sku
                  subtract unitsOnHand by quantity
                  decrement parcelsOpen
                from ShelfReset key sku
                  clear lastBatch
                  add receipts by 0
                  add parcelsOpen by 0
              specification ShelfFollowsStock
                given SkuRegistered
                  for "ACM-1"
                  sku = "ACM-1"
                given StockReceived
                  for "ACM-1"
                  sku = "ACM-1"
                  quantity = 40
                  batch = "B1"
                given StockReceived
                  for "ACM-1"
                  sku = "ACM-1"
                  quantity = 10
                  batch = "B2"
                given StockDispatched
                  for "ACM-1"
                  sku = "ACM-1"
                  quantity = 5
                then readmodel ShelfFigures
                  sku = "ACM-1"
                  receipts = 2
                  unitsOnHand = 45
                  lastBatch = "B2"
                  parcelsOpen = -1
              specification ResetClearsTheBatch
                given SkuRegistered
                  for "ACM-2"
                  sku = "ACM-2"
                given StockReceived
                  for "ACM-2"
                  sku = "ACM-2"
                  quantity = 7
                  batch = "B9"
                given ShelfReset
                  for "ACM-2"
                  sku = "ACM-2"
                then readmodel ShelfFigures
                  sku = "ACM-2"
                  receipts = 1
                  unitsOnHand = 7
        """;

    // Re-frozen from train 9 execution after combining generated documentation, concepts and shared Common output.
    const string FlatVersionThree = "279B82B5DB855622AF06A6F579461DCCEA8546CDE7275D236DFAAA782235AC27";
    const string RunningTotalsVersionThree = "E123F8C475E43B81F0CA41DA5DE40F7E5C91DAE109941D115CD7C2586F683774";
    const string RunningTotalsClassic = "E123F8C475E43B81F0CA41DA5DE40F7E5C91DAE109941D115CD7C2586F683774";

    internal static ArtifactRenderProfile VersionThree() =>
        CratisRendering.WithReducerConvention(
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            ReducerConventionInput.Read("Projects", /* lang=json */ "{\"version\":3,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\"}"));

    internal static string Shelf(ArtifactRenderPlan plan)
    {
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return string.Concat(plan.Artifacts
            .Where(artifact => artifact.RelativePath.Contains("Shelf", StringComparison.Ordinal) && artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .Select(artifact => artifact.RelativePath + "\n" + Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    static string Hash(string text) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    static string Projection(ArtifactRenderPlan plan)
    {
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Shelf/Shelf.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact]
    public void should_render_running_totals_and_clears_as_attributes()
    {
        var code = Projection(when_rendering_model_bound_projections.Plan(Source, true));
        Assert.DoesNotContain("#pragma", code, StringComparison.Ordinal);
        Assert.DoesNotContain("IProjectionFor<ShelfFigures>", code, StringComparison.Ordinal);
        Assert.DoesNotContain("NoAutoMap()", code, StringComparison.Ordinal);
        Assert.Contains("[NoAutoMap]", code, StringComparison.Ordinal);
        foreach (var @event in new[] { "SkuRegistered", "StockReceived", "StockDispatched", "ShelfReset" })
        {
            Assert.Contains($"FromEvent<{@event}>(nameof({@event}.Sku))", code, StringComparison.Ordinal);
        }

        Assert.Contains("[Increment<StockReceived>]", code, StringComparison.Ordinal);
        Assert.Contains("[AddFrom<StockReceived>(nameof(StockReceived.Quantity))]", code, StringComparison.Ordinal);
        Assert.Contains("[SubtractFrom<StockDispatched>(nameof(StockDispatched.Quantity))]", code, StringComparison.Ordinal);
        Assert.Contains("[ClearWith<ShelfReset>]", code, StringComparison.Ordinal);
        Assert.Contains("[Decrement<StockDispatched>]", code, StringComparison.Ordinal);
        Assert.DoesNotContain("AddFrom<ShelfReset>", code, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("product")]
    [InlineData("literal")]
    [InlineData("constant-key")]
    [InlineData("clear-only")]
    public void should_stay_fluent_for_shapes_version_four_cannot_express(string shape)
    {
        var source = shape switch
        {
            "product" => Source.Replace("add unitsOnHand by quantity\n", "add unitsOnHand by quantity * quantity\n", StringComparison.Ordinal),
            "literal" => Source.Replace("        lastBatch String optional\n", "        lastBatch String optional\n        label String optional\n", StringComparison.Ordinal)
                .Replace("        from SkuRegistered key sku\n          sku = sku\n", "        from SkuRegistered key sku\n          sku = sku\n          label = \"new\"\n", StringComparison.Ordinal)
                .Replace("          lastBatch = batch\n", "          lastBatch = batch\n          label = batch\n", StringComparison.Ordinal),
            "constant-key" => Source.Replace("from ShelfReset key sku\n", "from ShelfReset key \"global\"\n          sku = \"global\"\n", StringComparison.Ordinal),
            _ => Source.Replace("        lastBatch String optional\n", "        lastBatch String optional\n        note String optional\n", StringComparison.Ordinal)
                .Replace("          clear lastBatch\n", "          clear lastBatch\n          clear note\n", StringComparison.Ordinal)
        };
        Assert.NotEqual(Source, source);
        var code = Projection(when_rendering_model_bound_projections.Plan(source, true));
        Assert.Contains("IProjectionFor<ShelfFigures>", code, StringComparison.Ordinal);
    }

    [Fact]
    public void should_keep_version_three_bytes_for_running_totals()
    {
        Assert.Equal(RunningTotalsVersionThree, Hash(Shelf(when_rendering_model_bound_projections.Plan(Source, VersionThree()))));
    }

    [Fact]
    public void should_keep_version_three_bytes_for_flat_projections()
    {
        Assert.Equal(FlatVersionThree, Hash(when_rendering_model_bound_projections.Code(when_rendering_model_bound_projections.Plan(when_rendering_model_bound_projections.Source, VersionThree()))));
    }

    [Fact]
    public void should_keep_classic_bytes_for_running_totals()
    {
        Assert.Equal(RunningTotalsClassic, Hash(Shelf(when_rendering_model_bound_projections.Plan(Source, false))));
    }
}
#endif
