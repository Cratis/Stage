// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Captures;
using Cratis.Stage.Contracts.Captures;
using ScreenplayCaptureWhenKind = Cratis.Screenplay.Syntax.Captures.CaptureWhenKind;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <c>capture</c> declarations of a slice into Stage <see cref="CaptureDefinition"/>
/// records.
/// </summary>
/// <remarks>
/// The <c>map</c> body is carried alongside the appends rather than folded into them. A translation table or a
/// split is stated once and applies to every append below it, so flattening it onto each one would restate the
/// document and lose which appends actually share a mapping.
/// </remarks>
public static class CaptureConverter
{
    /// <summary>
    /// Converts a slice's capture declarations into their Stage records.
    /// </summary>
    /// <param name="captures">The capture declarations.</param>
    /// <param name="slicePath">The fully-qualified slice path, used to derive stable identifiers.</param>
    /// <returns>The Stage capture definitions, in declaration order.</returns>
    public static IReadOnlyList<CaptureDefinition> Convert(IEnumerable<CaptureSyntax> captures, string slicePath) =>
    [
        .. captures.Select(capture => new CaptureDefinition(
            DeterministicId.From($"{slicePath}.capture.{capture.Name}"),
            capture.Name,
            Source(capture.Source),
            capture.Key,
            Map(capture.Map),
            Appends(capture.Appends),
            [.. capture.Children.Select(children => new CaptureChildren(
                children.Property,
                children.IdentifiedBy,
                Map(children.Map),
                Appends(children.Appends)))],
            [.. capture.Nested.Select(nested => new CaptureNested(
                nested.Property,
                Map(nested.Map),
                Appends(nested.Appends)))]))
    ];

    static CaptureSource? Source(CaptureSourceSyntax? source) =>
        source is null
            ? null
            : new CaptureSource(source.Kind, [.. source.Settings.Select(setting => new CaptureSourceSetting(setting.Name, setting.Value))]);

    static IReadOnlyList<CaptureAppend> Appends(IEnumerable<CaptureAppendSyntax> appends) =>
    [
        .. appends.Select(append => new CaptureAppend(
            append.Event,
            When(append.When),
            [.. append.Mappings.Select(ProducedValueConverter.Property)],
            ProducedValueConverter.Tags(append.Tags)))
    ];

    static CaptureTrigger? When(CaptureWhenSyntax? when) =>
        when is null
            ? null
            : new CaptureTrigger(Kind(when.Kind), [.. when.Properties], when.FromValue, when.ToValue, when.Expression);

    static IReadOnlyList<CaptureMapOperation> Map(IEnumerable<CaptureMapOperationSyntax> operations) =>
        [.. operations.Select(Operation).OfType<CaptureMapOperation>()];

    // A map operation Stage has not been taught is left out rather than turned into the nearest one it has:
    // a mapping standing in for a split states a translation the document never wrote.
    static CaptureMapOperation? Operation(CaptureMapOperationSyntax operation) =>
        operation switch
        {
            CaptureMapEntrySyntax entry => Entry(entry),
            CaptureSplitSyntax split => Split(split),
            _ => null
        };

    static CaptureMapping Entry(CaptureMapEntrySyntax entry)
    {
        var (kind, expression) = ProducedValueConverter.Convert(entry.Source);

        return new(
            entry.Property,
            kind,
            expression,
            [.. entry.Translations.Select(translation => new CaptureTranslation(translation.From, translation.To))]);
    }

    static CaptureSplit Split(CaptureSplitSyntax split)
    {
        var (kind, expression) = ProducedValueConverter.Convert(split.Source);

        return new(kind, expression, split.Separator, [.. split.Targets]);
    }

    static CaptureTriggerKind Kind(ScreenplayCaptureWhenKind kind) =>
        kind switch
        {
            ScreenplayCaptureWhenKind.PropertyChanged => CaptureTriggerKind.PropertyChanged,
            ScreenplayCaptureWhenKind.Added => CaptureTriggerKind.Added,
            ScreenplayCaptureWhenKind.Removed => CaptureTriggerKind.Removed,
            ScreenplayCaptureWhenKind.Changed => CaptureTriggerKind.Changed,
            ScreenplayCaptureWhenKind.LogicalOr => CaptureTriggerKind.LogicalOr,
            ScreenplayCaptureWhenKind.LogicalAnd => CaptureTriggerKind.LogicalAnd,
            ScreenplayCaptureWhenKind.ValueTransition => CaptureTriggerKind.ValueTransition,
            ScreenplayCaptureWhenKind.Expression => CaptureTriggerKind.Expression,
            _ => CaptureTriggerKind.Changed
        };
}
