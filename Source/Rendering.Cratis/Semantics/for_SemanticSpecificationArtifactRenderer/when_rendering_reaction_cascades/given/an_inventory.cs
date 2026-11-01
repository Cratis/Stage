// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer.when_rendering_reaction_cascades.given;

/// <summary>
/// An inventory whose registration command produces an event that a reaction invokes a stock snapshot on.
/// </summary>
public class an_inventory : Specification
{
    /// <summary>
    /// The registration and the snapshot are separate slices: the reaction belongs to the Automation slice, not the command's.
    /// </summary>
    internal const string SeparateSlices = """
        module Inventory
          feature Stock
            slice StateChange RegisterSku
              command RegisterSku
                sku String identifier
                produces SkuRegistered
                  for sku
                  sku = sku
              event SkuRegistered
                sku String
              specification Registering
                when RegisterSku
                  sku = "widget"
                then SkuRegistered
                  sku = "widget"
            slice Automation SnapshotStock
              command RecordStockLevel
                sku String identifier
                produces StockLevelRecorded
                  for sku
              event StockLevelRecorded
              reaction Snapshot
                when SkuRegistered
                  invokes RecordStockLevel
                    sku = sku
              specification Snapshotting
                when append SkuRegistered
                  sku = "widget"
                then StockLevelRecorded
        """;

    /// <summary>
    /// The same behavior in one slice: the command's own slice reacts to its event, so its specification is a cascade.
    /// </summary>
    internal const string OneSlice = """
        module Inventory
          feature Stock
            slice Automation RegisterSku
              command RegisterSku
                sku String identifier
                produces SkuRegistered
                  for sku
                  sku = sku
              command RecordStockLevel
                sku String identifier
                produces StockLevelRecorded
                  for sku
              event SkuRegistered
                sku String
              event StockLevelRecorded
              reaction Snapshot
                when SkuRegistered
                  invokes RecordStockLevel
                    sku = sku
              specification Registering
                when RegisterSku
                  sku = "widget"
                then SkuRegistered
                  sku = "widget"
                then StockLevelRecorded
        """;

    private protected ArtifactRenderPlan _plan = null!;

    protected void Plan(string source)
    {
        var model = for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Inventory", "Inventory"));
    }

    protected IEnumerable<string> Paths(string fileName) =>
        _plan.Artifacts.Select(artifact => artifact.RelativePath).Where(path => path.EndsWith($"/{fileName}", StringComparison.Ordinal));

    protected string Source(string fileName) =>
        string.Concat(_plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith($"/{fileName}", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
