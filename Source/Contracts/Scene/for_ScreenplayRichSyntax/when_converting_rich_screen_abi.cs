// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Forms;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplayRichSyntax;

public class when_converting_rich_screen_abi : Specification
{
    BindingExpression _literalBinding = null!;
    CommandFormLayout _layout = null!;
    object? _literalValue;
    string _stableId = null!;
    Cratis.Scene.Model.Forms.FormGenerationMode? _generationMode;

    void Establish()
    {
        var literal = new LiteralExpressionSyntax(null, SourceLocation.Start);
        var binding = new RichBinding(
            BindingKind: "Literal",
            Path: string.Empty,
            Query: null,
            ComponentId: null,
            ComponentPropertyPath: null,
            Mode: "TwoWay",
            NullBehavior: "Clear",
            ExpectedValueType: "boolean",
            Literal: literal);

        var layout = new RichLayout(
            Columns:
            [
                new RichColumn(1, new RichWidth("Fraction", 1), new RichWidth("Pixels", 240), new RichWidth("Percent", 50)),
                new RichColumn(2, new RichWidth("Fraction", 2), null, null)
            ],
            Placements:
            [
                new RichPlacement("workItemId", 1, 1, null, null, new RichWidth("Auto", null)),
                new RichPlacement("title", 1, 2, null, 1, new RichWidth("Percent", 100))
            ],
            ColumnGap: new RichWidth("Pixels", 24),
            RowGap: new RichWidth("Pixels", 16));

        var form = new RichForm("Manual", layout);
        var component = new RichComponent("work-items:list");
        var property = new RichProperty(new LiteralExpressionSyntax(25, SourceLocation.Start));

        _literalBinding = ScreenplayRichSyntax.Binding(binding);
        _generationMode = ScreenplayRichSyntax.GenerationMode(form);
        _layout = ScreenplayRichSyntax.Layout(form)!;
        _stableId = ScreenplayRichSyntax.StableId(component, "workItems");
        _literalValue = ScreenplayRichSyntax.ComponentPropertyValue(property);
    }

    [Fact] void should_carry_literal_binding_kind() => _literalBinding.Kind.ShouldEqual(BindingSourceKind.Literal);
    [Fact] void should_carry_literal_binding_value() => _literalBinding.Value.ShouldBeNull();
    [Fact] void should_carry_binding_mode() => _literalBinding.Mode.ShouldEqual(BindingMode.TwoWay);
    [Fact] void should_carry_binding_null_behavior() => _literalBinding.NullBehavior.ShouldEqual(BindingNullBehavior.Clear);
    [Fact] void should_carry_expected_value_type() => _literalBinding.ExpectedValueType.ShouldEqual("boolean");
    [Fact] void should_carry_generation_mode() => _generationMode.ShouldEqual(Cratis.Scene.Model.Forms.FormGenerationMode.Manual);
    [Fact] void should_carry_layout_columns() => _layout.Columns.Count.ShouldEqual(2);
    [Fact] void should_carry_fractional_column_width() => _layout.Columns[0].Width!.Unit.ShouldEqual(FormWidthUnit.Fraction);
    [Fact] void should_carry_pixel_minimum_width() => _layout.Columns[0].MinWidth!.Value.ShouldEqual(240d);
    [Fact] void should_carry_percent_maximum_width() => _layout.Columns[0].MaxWidth!.Unit.ShouldEqual(FormWidthUnit.Percent);
    [Fact] void should_carry_layout_placements() => _layout.Placements.Count.ShouldEqual(2);
    [Fact] void should_carry_field_column_span() => _layout.Placements[1].ColumnSpan.ShouldEqual(1);
    [Fact] void should_carry_field_width() => _layout.Placements[1].Width!.Value.ShouldEqual(100d);
    [Fact] void should_carry_layout_gaps() => _layout.ColumnGap!.Value.ShouldEqual(24d);
    [Fact] void should_preserve_exact_stable_id() => _stableId.ShouldEqual("work-items:list");
    [Fact] void should_convert_expression_literal_values() => _literalValue.ShouldEqual(25);

    record RichBinding(
        string BindingKind,
        string Path,
        string? Query,
        string? ComponentId,
        string? ComponentPropertyPath,
        string? Mode,
        string? NullBehavior,
        string? ExpectedValueType,
        ExpressionSyntax? Literal);

    record RichForm(string GenerationMode, RichLayout Layout);

    record RichLayout(IReadOnlyList<RichColumn> Columns, IReadOnlyList<RichPlacement> Placements, RichWidth? ColumnGap, RichWidth? RowGap);

    record RichColumn(int Index, RichWidth? Width, RichWidth? MinWidth, RichWidth? MaxWidth);

    record RichPlacement(string Field, int Row, int Column, int? RowSpan, int? ColumnSpan, RichWidth? Width);

    record RichWidth(string Unit, double? Value);

    record RichComponent(string StableId);

    record RichProperty(ExpressionSyntax Value);
}
