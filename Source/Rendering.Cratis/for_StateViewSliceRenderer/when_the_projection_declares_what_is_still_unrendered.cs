// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// Rejects a root-level clear directive before rendering a partial read model.
/// </summary>
public class when_the_projection_declares_what_is_still_unrendered : Specification
{
    ApplicationSet _applicationSet = null!;
    Exception? _error;

    void Establish()
    {
        var lineAdded = new EventSyntax(
            "OrderLineAdded",
            [
                new PropertySyntax("orderNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
                new PropertySyntax("sku", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start);

        var from = new FromSyntax(
            [new EventSpecSyntax("OrderLineAdded", null, SourceLocation.Start)],
            null,
            null,
            [new SetMappingSyntax("orderNumber", new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var childFrom = new FromSyntax(
            [new EventSpecSyntax("OrderLineAdded", null, SourceLocation.Start)],
            null,
            null,
            [new SetMappingSyntax("sku", new PathExpressionSyntax("sku", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var children = new ChildrenSyntax(
            "lines",
            new PathExpressionSyntax("sku", SourceLocation.Start),
            AutoMapMode.Inherit,
            [childFrom],
            SourceLocation.Start);

        var nestedFrom = new FromSyntax(
            [new EventSpecSyntax("OrderLineAdded", null, SourceLocation.Start)],
            null,
            null,
            [new SetMappingSyntax("sku", new PathExpressionSyntax("sku", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var nested = new NestedSyntax("shipping", AutoMapMode.Inherit, [nestedFrom], SourceLocation.Start);

        var clearWith = new ClearWithSyntax("OrderArchived", SourceLocation.Start);

        var compositeKey = new CompositeKeySyntax(
            "OrderLineKey",
            [new KeyPartSyntax("orderNumber", new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "OrderLines",
            "OrderLines",
            null,
            AutoMapMode.Enabled,
            compositeKey,
            [from, children, nested, clearWith],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, "OrderLines", [lineAdded], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Orders", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Sales", [], [feature], SourceLocation.Start);
        _applicationSet = new ApplicationSet([new ApplicationSyntax([], [], [], [module], SourceLocation.Start)]);
    }

    void Because() => _error = Catch.Exception(() => new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp"));

    [Fact] void should_reject_root_level_clear() => _error.ShouldBeOfExactType<UnsupportedLegacyProjection>();
    [Fact] void should_name_the_blocking_diagnostic() => _error!.Message.ShouldContain(UnsupportedLegacyProjection.DiagnosticCode);
}
