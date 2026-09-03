// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;

/// <summary>
/// An order summary projection that uses every projection block with a model-bound attribute behind it: a
/// <c>from</c>, a <c>join</c> whose key is mapped by that <c>from</c> and which two events feed, an <c>all</c>
/// and an <c>every</c> block, and a <c>remove via join</c> carrying its own key.
/// </summary>
/// <remarks>
/// <c>clear with</c> is deliberately absent: Screenplay only accepts one inside a <c>nested</c> block, and
/// Chronicle only reads a class-level <c>[ClearWith]</c> on a nested type, so it has no rendering here. It is
/// covered as an unrendered block instead.
/// </remarks>
public class a_projection_using_every_model_bound_block : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _orderSlice = null!;

    void Establish()
    {
        var orderPlaced = new EventSyntax(
            "OrderPlaced",
            [
                Property("orderNumber", "String"),
                Property("customerId", "Uuid"),
            ],
            SourceLocation.Start);

        var customerRegistered = new EventSyntax("CustomerRegistered", [Property("name", "String")], SourceLocation.Start);
        var customerRenamed = new EventSyntax("CustomerRenamed", [Property("name", "String")], SourceLocation.Start);
        var customerAccountClosed = new EventSyntax("CustomerAccountClosed", [Property("customerId", "Uuid")], SourceLocation.Start);

        var placed = new FromSyntax(
            [new EventSpecSyntax("OrderPlaced", null, SourceLocation.Start)],
            null,
            null,
            [
                new SetMappingSyntax("orderNumber", new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start),
                new SetMappingSyntax("customerId", new PathExpressionSyntax("customerId", SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start);

        var join = new JoinSyntax(
            "customer",
            "customerId",
            [
                JoinedEvent("CustomerRegistered", "customerName", "name"),
                JoinedEvent("CustomerRenamed", "customerName", "name"),
            ],
            SourceLocation.Start);

        var all = new AllSyntax(
            [
                new SetMappingSyntax("lastEventAt", new EventContextExpressionSyntax("occurred", SourceLocation.Start), SourceLocation.Start),
                new CountMappingSyntax("totalEvents", SourceLocation.Start),
            ],
            AutoMapMode.Inherit,
            SourceLocation.Start);

        var every = new EverySyntax(
            [
                new SetMappingSyntax("lastUpdatedAt", new EventContextExpressionSyntax("occurred", SourceLocation.Start), SourceLocation.Start),
                new SetMappingSyntax("lastTouchedBy", new EventSourceIdExpressionSyntax(SourceLocation.Start), SourceLocation.Start),
            ],
            true,
            AutoMapMode.Inherit,
            SourceLocation.Start);

        var removeViaJoin = new RemoveViaJoinSyntax(
            "CustomerAccountClosed",
            new PathExpressionSyntax("customerId", SourceLocation.Start),
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "OrderSummary",
            "OrderSummary",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start),
            [placed, join, all, every, removeViaJoin],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView,
            "OrderSummary",
            [orderPlaced, customerRegistered, customerRenamed, customerAccountClosed],
            [],
            [],
            [projection],
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Orders", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Sales", [], [feature], SourceLocation.Start);

        _applicationSet = new ApplicationSet([new ApplicationSyntax([], [], [], [module], SourceLocation.Start)]);
        _orderSlice = _applicationSet.Slices.Single();
    }

    static PropertySyntax Property(string name, string type) =>
        new(name, new TypeRefSyntax(type, false, false, SourceLocation.Start), SourceLocation.Start);

    static JoinEventSyntax JoinedEvent(string @event, string property, string source) =>
        new(
            @event,
            AutoMapMode.Inherit,
            [new SetMappingSyntax(property, new PathExpressionSyntax(source, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);
}
