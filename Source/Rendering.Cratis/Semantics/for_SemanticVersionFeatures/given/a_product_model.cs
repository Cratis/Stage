// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.given;

/// <summary>
/// Compiles and renders small models that multiply in a projection.
/// </summary>
internal static class a_product_model
{
    /// <summary>
    /// An application where the basket lookup projection names a basket in one block and totals lines in another.
    /// </summary>
    internal const string Source = """
        concept BasketId : Uuid
        module Shop
          feature Baskets
            slice StateChange AddLine
              command AddLine
                basketId BasketId identifier
                unitPrice Decimal
                quantity Int
                produces LineAdded
                  for basketId
                  unitPrice = unitPrice
                  quantity = quantity
              event LineAdded
                unitPrice Decimal
                quantity Int
            slice StateChange NameBasket
              command NameBasket
                basketId BasketId identifier
                name String
                produces BasketNamed
                  for basketId
                  name = name
              event BasketNamed
                name String
            slice StateView BasketNames
              readmodel BasketName
                basketId BasketId key
                name String?
              query BasketNameById => BasketName optional
                by basketId BasketId
              projection BasketNameProjection => BasketName
                from BasketNamed
                  name = name
            slice StateView BasketLookup
              readmodel Basket
                basketId BasketId key
                name NAME_TYPE
                total Decimal?
              query BasketById => Basket optional
                by basketId BasketId
              projection BasketProjection => Basket
                from BasketNamed
                  name = name
                from LineAdded
                  add total by unitPrice * quantity
        """;

    /// <summary>
    /// Plans the application.
    /// </summary>
    /// <param name="source">The Screenplay source.</param>
    /// <returns>The render plan.</returns>
    internal static ArtifactRenderPlan Plan(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("shop"), "shop", "Shop.play", source);
        var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model, []).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Shop", "Shop"));
    }

    /// <summary>
    /// Plans an application that must render.
    /// </summary>
    /// <param name="source">The Screenplay source.</param>
    /// <returns>The successful render plan.</returns>
    internal static ArtifactRenderPlan Planned(string source)
    {
        var plan = Plan(source);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        return plan;
    }

    /// <summary>
    /// Gets the rendered text of one artifact.
    /// </summary>
    /// <param name="plan">The render plan.</param>
    /// <param name="relativePath">The artifact path.</param>
    /// <returns>The rendered text.</returns>
    internal static string Artifact(ArtifactRenderPlan plan, string relativePath) =>
        Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == relativePath).Bytes.AsSpan());
}
