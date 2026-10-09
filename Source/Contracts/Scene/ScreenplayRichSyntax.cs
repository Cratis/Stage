// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using SceneCommon = Cratis.Scene.Model.Common;
using SceneForms = Cratis.Scene.Model.Forms;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Reads the rich Screenplay screen ABI while Stage is between the released Screenplay package and the next
/// package that declares those members directly.
/// </summary>
/// <remarks>
/// The members here are Screenplay #592's public contract. Reflection is deliberately kept at this edge so the
/// actual Scene application plan remains typed and so the branch can build against the latest public Screenplay
/// package until #592 is published. Once Stage pins the published package, these helpers can collapse to direct
/// property access without changing the emitted plan.
/// </remarks>
static class ScreenplayRichSyntax
{
    const string LiteralKindName = "Literal";

    /// <summary>
    /// Gets the exact stable component id authored by Screenplay #592, falling back to the component name when
    /// the member is not present in the currently pinned Screenplay package.
    /// </summary>
    /// <param name="component">The component syntax.</param>
    /// <returns>The exact stable id to emit in Scene.</returns>
    public static string StableId(ScreenplaySyntax.ScreenComponentSyntax component) =>
        StableId(component, component.Name);

    /// <summary>
    /// Gets the exact stable component id from a shape-compatible Screenplay syntax object.
    /// </summary>
    /// <param name="component">The component syntax object.</param>
    /// <param name="fallbackName">The authored component name used when no explicit stable id exists.</param>
    /// <returns>The exact stable id to emit in Scene.</returns>
    public static string StableId(object component, string fallbackName) =>
        StringProperty(component, "StableId") is { Length: > 0 } stableId ? stableId : fallbackName;

    /// <summary>
    /// Converts a Screenplay UI binding to the Scene binding ABI.
    /// </summary>
    /// <param name="binding">The Screenplay binding.</param>
    /// <returns>The Scene binding expression.</returns>
    public static SceneCommon.BindingExpression Binding(ScreenplaySyntax.UiBindingSyntax binding) =>
        Binding((object)binding);

    /// <summary>
    /// Converts a shape-compatible Screenplay UI binding to the Scene binding ABI.
    /// </summary>
    /// <param name="binding">The Screenplay binding object.</param>
    /// <returns>The Scene binding expression.</returns>
    public static SceneCommon.BindingExpression Binding(object binding) =>
        new(
            StringProperty(binding, "Path") ?? string.Empty,
            BindingKind(EnumName(Property(binding, "BindingKind"))),
            StringProperty(binding, "Query"),
            StringProperty(binding, "ComponentId"),
            StringProperty(binding, "ComponentPropertyPath"),
            BindingMode(EnumName(Property(binding, "Mode"))),
            BindingNullBehavior(EnumName(Property(binding, "NullBehavior"))),
            StringProperty(binding, "ExpectedValueType"),
            Literal(binding));

    /// <summary>
    /// Converts a Screenplay component property literal value to the Scene property value ABI.
    /// </summary>
    /// <param name="property">The Screenplay component property.</param>
    /// <returns>The converted value.</returns>
    public static object? ComponentPropertyValue(ScreenplaySyntax.ComponentPropertySyntax property) =>
        ComponentPropertyValue((object)property);

    /// <summary>
    /// Converts a shape-compatible Screenplay component property literal value to the Scene property value ABI.
    /// </summary>
    /// <param name="property">The Screenplay component property object.</param>
    /// <returns>The converted value.</returns>
    public static object? ComponentPropertyValue(object property) =>
        ExpressionValue(Property(property, "Value"));

    /// <summary>
    /// Gets the form generation mode introduced by Screenplay #592.
    /// </summary>
    /// <param name="form">The form syntax.</param>
    /// <returns>The Scene generation mode, or <c>null</c> when not declared.</returns>
    public static SceneForms.FormGenerationMode? GenerationMode(ScreenplaySyntax.FormSyntax form) =>
        GenerationMode((object)form);

    /// <summary>
    /// Gets the form generation mode from a shape-compatible Screenplay form syntax object.
    /// </summary>
    /// <param name="form">The form syntax object.</param>
    /// <returns>The Scene generation mode, or <c>null</c> when not declared.</returns>
    public static SceneForms.FormGenerationMode? GenerationMode(object form) =>
        EnumName(Property(form, "GenerationMode")) switch
        {
            "Auto" => SceneForms.FormGenerationMode.Auto,
            "Manual" => SceneForms.FormGenerationMode.Manual,
            _ => null,
        };

    /// <summary>
    /// Converts a Screenplay #592 command form layout to the Scene form layout ABI.
    /// </summary>
    /// <param name="form">The form syntax.</param>
    /// <returns>The converted layout, or <c>null</c> when no layout was authored.</returns>
    public static SceneForms.CommandFormLayout? Layout(ScreenplaySyntax.FormSyntax form) =>
        Layout((object)form);

    /// <summary>
    /// Converts a Screenplay #592 command form layout from a shape-compatible form syntax object to the Scene
    /// form layout ABI.
    /// </summary>
    /// <param name="form">The form syntax object.</param>
    /// <returns>The converted layout, or <c>null</c> when no layout was authored.</returns>
    public static SceneForms.CommandFormLayout? Layout(object form) =>
        Property(form, "Layout") is { } layout
            ? new SceneForms.CommandFormLayout(
                [.. Sequence(Property(layout, "Columns")).Select(Column)],
                [.. Sequence(Property(layout, "Placements")).Select(Placement)],
                Width(Property(layout, "ColumnGap")),
                Width(Property(layout, "RowGap")))
            : null;

    /// <summary>
    /// Converts the Screenplay field placement attached to a field when present.
    /// </summary>
    /// <param name="field">The field syntax.</param>
    /// <returns>The converted placement, or <c>null</c> when not authored.</returns>
    public static SceneForms.FormFieldPlacement? FieldPlacement(ScreenplaySyntax.FormFieldSyntax field) =>
        Property(field, "Placement") is { } placement ? Placement(placement) : null;

    static SceneCommon.BindingSourceKind BindingKind(string? kind) =>
        kind switch
        {
            nameof(ScreenplaySyntax.UiBindingKind.QueryResult) => SceneCommon.BindingSourceKind.QueryResult,
            nameof(ScreenplaySyntax.UiBindingKind.ComponentProperty) => SceneCommon.BindingSourceKind.ComponentProperty,
            LiteralKindName => SceneCommon.BindingSourceKind.Literal,
            _ => SceneCommon.BindingSourceKind.DataContext,
        };

    static SceneCommon.BindingMode? BindingMode(string? mode) =>
        mode switch
        {
            nameof(ScreenplaySyntax.UiBindingMode.TwoWay) => SceneCommon.BindingMode.TwoWay,
            nameof(ScreenplaySyntax.UiBindingMode.OneWay) => SceneCommon.BindingMode.OneWay,
            _ => null,
        };

    static SceneCommon.BindingNullBehavior? BindingNullBehavior(string? behavior) =>
        behavior switch
        {
            nameof(ScreenplaySyntax.UiBindingNullBehavior.Clear) => SceneCommon.BindingNullBehavior.Clear,
            nameof(ScreenplaySyntax.UiBindingNullBehavior.Preserve) => SceneCommon.BindingNullBehavior.Preserve,
            nameof(ScreenplaySyntax.UiBindingNullBehavior.Propagate) => SceneCommon.BindingNullBehavior.Propagate,
            _ => null,
        };

    static object? Literal(object binding) =>
        EnumName(Property(binding, "BindingKind")) == LiteralKindName ? ExpressionValue(Property(binding, "Literal")) : null;

    static SceneForms.FormColumn Column(object column) =>
        new(IntProperty(column, "Index"), Width(Property(column, "Width")), Width(Property(column, "MinWidth")), Width(Property(column, "MaxWidth")));

    static SceneForms.FormFieldPlacement Placement(object placement) =>
        new(
            StringProperty(placement, "Field") ?? string.Empty,
            IntProperty(placement, "Row"),
            IntProperty(placement, "Column"),
            NullableIntProperty(placement, "RowSpan"),
            NullableIntProperty(placement, "ColumnSpan"),
            Width(Property(placement, "Width")));

    static SceneForms.FormWidth? Width(object? width) =>
        width is null
            ? null
            : new SceneForms.FormWidth(WidthUnit(width), DoubleProperty(width, "Value"));

    static SceneForms.FormWidthUnit WidthUnit(object width) =>
        EnumName(Property(width, "Unit")) switch
        {
            "Pixels" => SceneForms.FormWidthUnit.Pixels,
            "Percent" => SceneForms.FormWidthUnit.Percent,
            "Auto" => SceneForms.FormWidthUnit.Auto,
            _ => SceneForms.FormWidthUnit.Fraction,
        };

    static object? ExpressionValue(object? value) =>
        value switch
        {
            null => null,
            string text => text,
            ScreenplaySyntax.LiteralExpressionSyntax literal => literal.Value,
            ScreenplaySyntax.ExpressionSyntax expression => new Dictionary<string, object?> { ["kind"] = expression.GetType().Name },
            _ => value,
        };

    static IEnumerable<object> Sequence(object? value) =>
        value is System.Collections.IEnumerable sequence
            ? sequence.Cast<object>()
            : [];

    static string? StringProperty(object source, string name) =>
        Property(source, name) as string;

    static string? EnumName(object? value) =>
        value?.ToString();

    static int IntProperty(object source, string name) =>
        Property(source, name) is int value ? value : 0;

    static int? NullableIntProperty(object source, string name) =>
        Property(source, name) is int value ? value : null;

    static double? DoubleProperty(object source, string name) =>
        Property(source, name) switch
        {
            double value => value,
            float value => value,
            decimal value => (double)value,
            int value => value,
            _ => null,
        };

    static object? Property(object source, string name) =>
        source.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
}
