// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticProjectionProducts;

/// <summary>
/// ESM v29 <c language="csharp">add addedValue by unitPrice * quantity</c>: Chronicle adds one event property, so the renderer gives the event the product
/// as a member and the projection adds that member; the generated application compiles.
/// </summary>
public class when_rendering_a_total_that_adds_a_product
{
    // Money x Quantity into a Money total (decimal), Quantity x Quantity into a whole total, and a subtract by the same product.
    public const string Source = """
        concept BasketId : Uuid
        concept Money : Decimal
          validate
            >= 0  message "An amount cannot be negative"
            decimals <= 2  message "An amount has at most two decimals"
        concept Quantity : Int
          validate
            > 0  message "A quantity is at least one"

        module Shop
          feature Basket
            slice StateChange AddLine
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
              event LineRemoved
                quantity  Quantity
                unitPrice Money
                pieces    Int
              specification AddingALine
                when AddLine
                  basketId  = "b1a4e6f8-1b3d-4f5a-9c7e-2d4f6a8b0c02"
                  quantity  = 3
                  unitPrice = 19.5
                then LineAdded
                  for "b1a4e6f8-1b3d-4f5a-9c7e-2d4f6a8b0c02"
                  quantity  = 3
                  unitPrice = 19.5

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
        """;

    [Fact]
    public void should_plan_the_application()
    {
        var plan = Plan(Source);

        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-050" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    }

    [Fact]
    public void should_give_the_event_the_product_as_a_serialized_member()
    {
        var generated = string.Join('\n', CSharp(Plan(Source)).Select(file => file.Content));

        Assert.Contains("public record LineAdded(", generated, StringComparison.Ordinal);
        Assert.Contains("public decimal UnitPriceTimesQuantity { get; init; } = (decimal)UnitPrice.Value * (decimal)Quantity.Value;", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void should_add_and_subtract_the_member_instead_of_one_factor()
    {
        var generated = string.Join('\n', CSharp(Plan(Source)).Select(file => file.Content));

        Assert.Contains("Add(model => model.AddedValue).With(evt => evt.UnitPriceTimesQuantity)", generated, StringComparison.Ordinal);
        Assert.Contains("Subtract(model => model.AddedValue).With(evt => evt.UnitPriceTimesQuantity)", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("With(evt => evt.UnitPrice)", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void should_compile()
    {
        Assert.Empty(RenderedOutput.Errors(CSharp(Plan(Source))));
    }

    [Fact]
    public void should_multiply_whole_numbers_checked_into_a_whole_total()
    {
        var plan = Plan(Whole("pieces Int optional", "Int"));

        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        var generated = string.Join('\n', CSharp(plan).Select(file => file.Content));
        Assert.Contains("public long CasesTimesPerCase { get; init; } = checked(Cases * PerCase);", generated.Substring(generated.IndexOf("record Packed", StringComparison.Ordinal)), StringComparison.Ordinal);
        Assert.Contains("Add(model => model.Pieces).With(evt => evt.CasesTimesPerCase)", generated, StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(CSharp(plan)));
    }

    [Fact]
    public void should_refuse_a_total_of_another_number_type_than_the_product()
    {
        var plan = Plan(Whole("pieces Decimal optional", "Int"));

        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-050" && diagnostic.Message.Contains("'pieces'", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_a_factor_that_may_be_absent()
    {
        var plan = Plan(Whole("pieces Int optional", "Int optional"));

        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-050" && diagnostic.Message.Contains("required whole or decimal numbers", StringComparison.Ordinal));
    }

    static string Whole(string total, string perCase) => $"""
        module Shop
          feature Stock
            slice StateChange Pack
              command Pack
                id       Uuid identifier
                cases    Int
                perCase  {perCase}
                produces Packed
                  for id
                  cases   = cases
                  perCase = perCase
              event Packed
                cases    Int
                perCase  {perCase}

            slice StateView Pieces
              readmodel Stock
                id     Uuid key
                {total}
              query GetStock => Stock optional
                by id Uuid
              projection StockProjection => Stock
                from Packed
                  add pieces by cases * perCase
        """;

    internal static ArtifactRenderPlan Plan(string source) => invoice_model.Plan(invoice_model.Compile(source));

    internal static RenderedFile[] CSharp(ArtifactRenderPlan plan) =>
    [
        .. plan.Artifacts
            .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))
    ];
}
