// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_mapping_command_properties_to_different_event_concepts : Specification
{
    static readonly Dictionary<string, object> _values = new(StringComparer.Ordinal)
    {
        ["Uuid"] = Guid.Parse("6a45d535-d6c3-4ae0-bc66-306b59a51d6c"),
        ["String"] = "stock-42",
        ["Int"] = 42,
        ["Decimal"] = 42.75m,
        ["Bool"] = true,
        ["Date"] = new DateOnly(2026, 10, 26),
        ["DateTime"] = new DateTimeOffset(2026, 10, 26, 12, 34, 56, TimeSpan.Zero),
    };

    [Theory]
    [InlineData("OrderId", "MovementId", "Uuid", "new MovementId(OrderId.Value)")]
    [InlineData("OrderId", "OrderId", "Uuid", "OrderId")]
    [InlineData("Uuid", "Uuid", "Uuid", "OrderId")]
    [InlineData("Uuid", "MovementId", "Uuid", "OrderId")]
    [InlineData("OrderId", "Uuid", "Uuid", "OrderId")]
    [InlineData("OrderId", "MovementId", "String", "new MovementId(OrderId.Value)")]
    [InlineData("OrderId", "MovementId", "Int", "new MovementId(OrderId.Value)")]
    [InlineData("OrderId", "MovementId", "Decimal", "new MovementId(OrderId.Value)")]
    [InlineData("OrderId", "MovementId", "Bool", "new MovementId(OrderId.Value)")]
    [InlineData("OrderId", "MovementId", "Date", "new MovementId(OrderId.Value)")]
    [InlineData("OrderId", "MovementId", "DateTime", "new MovementId(OrderId.Value)")]
    [InlineData("Int", "Decimal", "Int/Decimal", "OrderId")]
    [InlineData("OrderId", "Decimal", "Int/Decimal", "OrderId")]
    [InlineData("Int", "MovementId", "Int/Decimal", "OrderId")]
    [InlineData("OrderId", "MovementId", "Int/Decimal", "new MovementId(OrderId.Value)")]
    public void should_compile_and_retain_the_mapped_value(string sourceType, string targetType, string primitive, string argument)
    {
        var primitiveTypes = primitive.Split('/');
        var sourcePrimitive = primitiveTypes[0];
        var targetPrimitive = primitiveTypes[^1];
        var (files, commandFile) = Render(sourceType, targetType, (sourcePrimitive, targetPrimitive));
        commandFile.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
        commandFile.Content.ShouldContain($"new StockMoved({argument})");
        string.Join(Environment.NewLine, RenderedOutput.Errors(files)).ShouldEqual(string.Empty);
        var assembly = RenderedOutput.Load(files);
        var commandType = assembly.GetTypes().Single(type => type.Name == "MoveStock");
        var sourceValue = _values[sourcePrimitive];
        var expected = sourcePrimitive == targetPrimitive ? sourceValue : 42m;
        var input = commandType.GetConstructors().Single().GetParameters()[1].ParameterType;
        var source = sourceType == "OrderId" ? Activator.CreateInstance(input, sourceValue) : sourceValue;
        var command = Activator.CreateInstance(commandType, "destination", source);
        var produced = (EventForEventSourceId)commandType.GetMethod("Handle").Invoke(command, []);
        var result = produced.Event.GetType().GetProperty("MovementId").GetValue(produced.Event);
        var actual = string.Equals(targetType, "OrderId", StringComparison.Ordinal) || string.Equals(targetType, "MovementId", StringComparison.Ordinal)
            ? result.GetType().GetProperty("Value").GetValue(result)
            : result;
        actual.ShouldEqual(expected);
        produced.EventSourceId.ToString().ShouldEqual("destination");
    }

    [Theory]
    [InlineData("OrderId?", "MovementId?")]
    [InlineData("OrderId[]", "MovementId[]")]
    public void should_leave_optional_and_collection_concept_expressions_unchanged(string sourceType, string targetType)
    {
        var (_, commandFile) = Render(sourceType, targetType, ("Uuid", "Uuid"));
        commandFile.Content.ShouldContain("new StockMoved(OrderId)");
        commandFile.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
    }

    [Fact]
    public void should_resolve_the_leaf_type_of_a_nested_command_path()
    {
        var (files, commandFile) = Render("OrderId", "MovementId", ("Uuid", "Uuid"), nested: true);
        commandFile.Content.ShouldContain("new MovementId(Transfer.OrderId.Value)");
        commandFile.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
        string.Join(Environment.NewLine, RenderedOutput.Errors(files)).ShouldEqual(string.Empty);
        var assembly = RenderedOutput.Load(files);
        var expected = _values["Uuid"];
        var order = Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == "OrderId"), expected);
        var transfer = Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == "Transfer"), order);
        var commandType = assembly.GetTypes().Single(type => type.Name == "MoveStock");
        var command = Activator.CreateInstance(commandType, "destination", transfer);
        var produced = (EventForEventSourceId)commandType.GetMethod("Handle").Invoke(command, []);
        var result = produced.Event.GetType().GetProperty("MovementId").GetValue(produced.Event);
        result.GetType().GetProperty("Value").GetValue(result).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("OrderId", "MovementId", "Uuid", "String")]
    [InlineData("Uuid", "MovementId", "Uuid", "String")]
    [InlineData("OrderId", "String", "Uuid", "String")]
    [InlineData("OrderId", "MovementId", "Decimal", "Int")]
    [InlineData("Decimal", "MovementId", "Decimal", "Int")]
    [InlineData("OrderId", "Int", "Decimal", "Int")]
    public void should_report_incompatible_underlying_primitives_instead_of_coercing_them(string sourceType, string targetType, string sourcePrimitive, string targetPrimitive)
    {
        var (files, commandFile) = Render(sourceType, targetType, (sourcePrimitive, targetPrimitive));
        var sourceClrType = sourcePrimitive == "Uuid" ? "Guid" : "decimal";
        var targetClrType = targetPrimitive == "String" ? "string" : "int";
        commandFile.Content.ShouldContain("new StockMoved(default!)");
        commandFile.Content.ShouldNotContain("new MovementId(OrderId.Value)");
        commandFile.Diagnostics.ShouldContain(
            $"'movementId' is mapped from 'orderId', declared as '{sourceType}' — a {sourceClrType}, which the event declares as '{targetType}' — a {targetClrType} — rendered as a missing value.");
        string.Join(Environment.NewLine, RenderedOutput.Errors(files)).ShouldEqual(string.Empty);
    }

    static (RenderedFile[] Files, RenderedFile CommandFile) Render(string sourceType, string targetType, (string Source, string Target) primitives, bool nested = false)
    {
        var location = SourceLocation.Start;
        var orderId = Property("orderId", sourceType);
        var transfer = new TypeSyntax("Transfer", [orderId], location);
        var command = new CommandSyntax(
            "MoveStock",
            [Property("id", "String") with { IsIdentifier = true }, nested ? Property("transfer", "Transfer") : orderId],
            null,
            [],
            [new ProducesSyntax(
                "StockMoved",
                null,
                [new PropertyMappingSyntax("movementId", new PathExpressionSyntax(nested ? "transfer.orderId" : "orderId", location), location)],
                location)
                { For = new PathExpressionSyntax("id", location) }],
            null,
            location);
        var slice = new SliceSyntax(SliceType.StateChange, "Move", [new EventSyntax("StockMoved", [Property("movementId", targetType)], location)], [command], [], [], [], [], [], [], [], location);
        var application = new ApplicationSyntax(
            [],
            [
                new ConceptSyntax("OrderId", primitives.Source, [], [], location),
                new ConceptSyntax("MovementId", primitives.Target, [], [], location)
            ],
            [],
            [new ModuleSyntax("Inventory", [], [new FeatureSyntax("Stock", [], [slice], location)], location)],
            location,
            Types: nested ? [transfer] : []);
        var set = new ApplicationSet([application]);
        var commandFile = new StateChangeSliceRenderer().Render(set.Slices.Single(), set, "Generated");
        RenderedFile[] files = [
            .. set.Concepts.Values.Select(concept => ConceptRenderer.Render(concept, set, "Generated")),
            .. set.Types.Values.Select(type => TypeRenderer.Render(type, set, "Generated")),
            commandFile
        ];
        return (files, commandFile);
    }

    static PropertySyntax Property(string name, string type) =>
        new(name, new TypeRefSyntax(type.TrimEnd('?', '[', ']'), type.EndsWith("[]", StringComparison.Ordinal), type.EndsWith('?'), SourceLocation.Start), SourceLocation.Start);
}
