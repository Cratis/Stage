// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneSizeClasses = Cratis.Scene.Model.SizeClasses;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts the slot arrangements shared by layouts, screen templates and dialog templates.
/// </summary>
public static class ArrangementConverter
{
    /// <summary>
    /// Identifies an unsupported arrangement node.
    /// </summary>
    public const string NodeDiagnosticCode = "STAGE-SCENE-ARRANGEMENT-001";

    /// <summary>
    /// Identifies an unsupported container kind.
    /// </summary>
    public const string ContainerDiagnosticCode = "STAGE-SCENE-ARRANGEMENT-002";

    /// <summary>
    /// Identifies an invalid size or size class.
    /// </summary>
    public const string SizeDiagnosticCode = "STAGE-SCENE-ARRANGEMENT-004";

    /// <summary>
    /// Converts an arrangement using the legacy throwing behavior.
    /// </summary>
    /// <param name="arrangement">The arrangement, or null when none is declared.</param>
    /// <returns>The converted arrangement, or null.</returns>
    public static SceneLayouts.Arrangement? Convert(ScreenplaySyntax.ArrangementSyntax? arrangement) => new Conversion(null, string.Empty).Convert(arrangement);

    /// <summary>
    /// Converts an arrangement, omitting unsupported subtrees and reporting their source locations.
    /// </summary>
    /// <param name="arrangement">The arrangement, or null when none is declared.</param>
    /// <param name="diagnostics">The append-only diagnostic collector.</param>
    /// <param name="structure">The owning layout or template.</param>
    /// <returns>The arrangement with supported subtrees, or null if its root cannot be converted.</returns>
    public static SceneLayouts.Arrangement? Convert(ScreenplaySyntax.ArrangementSyntax? arrangement, ConversionDiagnostics diagnostics, string structure) =>
        new Conversion(diagnostics, structure).Convert(arrangement);

    /// <summary>
    /// Converts a Scene arrangement back into the Screenplay syntax it is written as, so a layout or template a Scene document declares can be exported as typed Screenplay.
    /// </summary>
    /// <param name="arrangement">The Scene arrangement, or null when none is declared.</param>
    /// <returns>The Screenplay arrangement, or null when there is none to write.</returns>
    /// <remarks>
    /// The inverse of <see cref="Convert(ScreenplaySyntax.ArrangementSyntax?)"/>: converting the result again gives the arrangement it was made from. A slot a freeform
    /// variant does not place is left out of the variant, as a hidden place is converted to nothing.
    /// </remarks>
    public static ScreenplaySyntax.ArrangementSyntax? ToSyntax(SceneLayouts.Arrangement? arrangement) => arrangement switch
    {
        SceneLayouts.FlowArrangement flow => new ScreenplaySyntax.ArrangementSyntax(
            ScreenplaySyntax.ArrangementMode.Flow,
            SourceLocation.Start,
            ToRootSyntax(flow.Root),
            [.. (flow.Overrides ?? []).Select(over => new ScreenplaySyntax.ArrangementOverrideSyntax(
                over.Width?.ToString().ToLowerInvariant(),
                over.Height?.ToString().ToLowerInvariant(),
                ToRootSyntax(over.Root),
                SourceLocation.Start))],
            null),
        SceneLayouts.FreeformSlotArrangement freeform => new ScreenplaySyntax.ArrangementSyntax(
            ScreenplaySyntax.ArrangementMode.Freeform,
            SourceLocation.Start,
            null,
            null,
            [.. freeform.Variants.Select(variant => new ScreenplaySyntax.VariantSyntax(
                variant.SizeClass.Width.ToString().ToLowerInvariant(),
                variant.SizeClass.Height.ToString().ToLowerInvariant(),
                [.. variant.Placements.Select(place => new ScreenplaySyntax.PlaceSyntax(
                    place.SlotName,
                    SourceLocation.Start,
                    Hidden: false,
                    X: (int)Math.Round(place.X, MidpointRounding.AwayFromZero),
                    Y: (int)Math.Round(place.Y, MidpointRounding.AwayFromZero),
                    SizeWidth: SizeText(place.Width),
                    SizeHeight: SizeText(place.Height)))],
                SourceLocation.Start))]),
        _ => null
    };

    // The Screenplay text states the nodes of a root without a container of its own, and reading it back wraps them in a plain column, so a plain column root is written as
    // its children. Any other root is written as it is and read back inside that column.
    static ScreenplaySyntax.ArrangementContainerSyntax ToRootSyntax(SceneLayouts.FlowNode root) =>
        root is SceneLayouts.FlowColumn { Gap: 0, Grow: null, Span: null } column
            ? new ScreenplaySyntax.ArrangementContainerSyntax(ScreenplaySyntax.ArrangementContainerKind.Flat, [.. column.Children.Select(ToSyntax)], SourceLocation.Start)
            : new ScreenplaySyntax.ArrangementContainerSyntax(ScreenplaySyntax.ArrangementContainerKind.Flat, [ToSyntax(root)], SourceLocation.Start);

    static string? SizeText(double size) => size == 0 ? "fill" : size.ToString(CultureInfo.InvariantCulture);

    static ScreenplaySyntax.ArrangementNodeSyntax ToSyntax(SceneLayouts.FlowNode node) => node switch
    {
        SceneLayouts.FlowSlotLeaf leaf => new ScreenplaySyntax.ArrangementSlotSyntax(leaf.SlotName, SourceLocation.Start, Width: null, Height: null, Grow: leaf.Grow is not null, Span: leaf.Span)
        {
            GrowFactor = leaf.Grow is > 1 ? leaf.Grow : null
        },
        SceneLayouts.FlowContainer container => new ScreenplaySyntax.ArrangementContainerSyntax(
            container.Kind switch
            {
                SceneLayouts.FlowContainerKind.Row => ScreenplaySyntax.ArrangementContainerKind.Row,
                SceneLayouts.FlowContainerKind.Grid => ScreenplaySyntax.ArrangementContainerKind.Grid,
                _ => ScreenplaySyntax.ArrangementContainerKind.Column
            },
            [.. container.Children.Select(ToSyntax)],
            SourceLocation.Start,
            Gap: container.Gap == 0 ? null : (int?)Math.Round((double)container.Gap, MidpointRounding.AwayFromZero))
        {
            Grow = container.Grow,
            Span = container.Span,
            Columns = (container as SceneLayouts.FlowGrid)?.Columns,
            Rows = (container as SceneLayouts.FlowGrid)?.Rows
        },
        _ => throw new UnknownArrangementNode(node.GetType().Name)
    };

    sealed class Conversion(ConversionDiagnostics? diagnostics, string structure)
    {
        public SceneLayouts.Arrangement? Convert(ScreenplaySyntax.ArrangementSyntax? arrangement) => arrangement switch
        {
            { Mode: ScreenplaySyntax.ArrangementMode.Flow, Root: not null } => Flow(arrangement),
            { Mode: ScreenplaySyntax.ArrangementMode.Freeform, Variants: not null } => new SceneLayouts.FreeformSlotArrangement([.. arrangement.Variants.Select(Variant).OfType<SceneLayouts.FreeformSlotVariant>()]),
            _ => null
        };

        SceneLayouts.FlowArrangement? Flow(ScreenplaySyntax.ArrangementSyntax arrangement)
        {
            var root = Node(arrangement.Root!);
            var overrides = (arrangement.Overrides ?? []).Select(Override).OfType<SceneLayouts.FlowOverride>().ToArray();
            return root is null ? null : new(root, overrides);
        }

        SceneLayouts.FlowOverride? Override(ScreenplaySyntax.ArrangementOverrideSyntax syntax)
        {
            var validWidth = Width(syntax.Width, syntax.Location, out var width);
            var validHeight = Height(syntax.Height, syntax.Location, out var height);
            var root = Node(syntax.Root);
            return validWidth && validHeight && root is not null ? new(width, height, root) : null;
        }

        SceneLayouts.FlowNode? Node(ScreenplaySyntax.ArrangementNodeSyntax node)
        {
            switch (node)
            {
                case ScreenplaySyntax.ArrangementSlotSyntax slot:
                    return new SceneLayouts.FlowSlotLeaf(slot.Name) { Grow = slot.GrowFactor ?? (slot.Grow ? 1 : null), Span = slot.Span };
                case ScreenplaySyntax.ArrangementContainerSyntax container:
                    return Container(container);
                default:
                    if (diagnostics is null) throw new UnknownArrangementNode(node.GetType().Name);
                    Report(NodeDiagnosticCode, $"arrangement node {node.GetType().Name}", node.Location);
                    return null;
            }
        }

        SceneLayouts.FlowContainer? Container(ScreenplaySyntax.ArrangementContainerSyntax container)
        {
            var children = container.Children.Select(Node).OfType<SceneLayouts.FlowNode>().ToList();
            var gap = container.Gap ?? 0;

            // Flat has no Scene counterpart; preserve the legacy column translation.
            SceneLayouts.FlowContainer? converted = container.Kind switch
            {
                ScreenplaySyntax.ArrangementContainerKind.Row => new SceneLayouts.FlowRow { Gap = gap, Children = children, Grow = container.Grow, Span = container.Span },
                ScreenplaySyntax.ArrangementContainerKind.Column or ScreenplaySyntax.ArrangementContainerKind.Flat => new SceneLayouts.FlowColumn { Gap = gap, Children = children, Grow = container.Grow, Span = container.Span },
                ScreenplaySyntax.ArrangementContainerKind.Grid => new SceneLayouts.FlowGrid { Gap = gap, Children = children, Grow = container.Grow, Span = container.Span, Columns = container.Columns, Rows = container.Rows },
                _ => null
            };
            if (converted is not null) return converted;
            if (diagnostics is null) throw new UnknownArrangementContainerKind(container.Kind);
            Report(ContainerDiagnosticCode, $"arrangement container kind {container.Kind}", container.Location);
            return null;
        }

        SceneLayouts.FreeformSlotVariant? Variant(ScreenplaySyntax.VariantSyntax variant)
        {
            var validWidth = Width(variant.Width, variant.Location, out var width);
            var validHeight = Height(variant.Height, variant.Location, out var height);
            var places = variant.Places.Where(place => !place.Hidden).Select(Placement).OfType<SceneLayouts.SlotPlacement>().ToArray();
            return validWidth && validHeight ? new(new(width!.Value, height!.Value), places) : null;
        }

        SceneLayouts.SlotPlacement? Placement(ScreenplaySyntax.PlaceSyntax place)
        {
            var validWidth = Size(place.SizeWidth, place.Location, out var width);
            var validHeight = Size(place.SizeHeight, place.Location, out var height);
            return validWidth && validHeight ? new(place.SlotName, place.X ?? 0, place.Y ?? 0, width, height) : null;
        }

        bool Size(string? text, SourceLocation location, out double size)
        {
            size = 0;
            if (text is null || text == "fill") return true;
            if (diagnostics is null)
            {
                size = double.Parse(text);
                return true;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out size) && double.IsFinite(size)) return true;
            Report(SizeDiagnosticCode, $"arrangement size '{text}'", location);
            return false;
        }

        bool Width(string? text, SourceLocation location, out SceneSizeClasses.WidthSizeClass? width)
        {
            width = null;
            if (text is null) return true;
            if (diagnostics is null)
            {
                width = SizeClassNames.ParseWidth(text);
                return true;
            }
            if (SizeClassNames.TryParseWidth(text, out var parsed))
            {
                width = parsed;
                return true;
            }
            Report(SizeDiagnosticCode, $"size class '{text}'", location);
            return false;
        }

        bool Height(string? text, SourceLocation location, out SceneSizeClasses.HeightSizeClass? height)
        {
            height = null;
            if (text is null) return true;
            if (diagnostics is null)
            {
                height = SizeClassNames.ParseHeight(text);
                return true;
            }
            if (SizeClassNames.TryParseHeight(text, out var parsed))
            {
                height = parsed;
                return true;
            }
            Report(SizeDiagnosticCode, $"size class '{text}'", location);
            return false;
        }

        void Report(string code, string construct, SourceLocation location) => diagnostics!.Report(new(
            code,
            DiagnosticSeverity.Error,
            construct,
            structure,
            location,
            $"Unsupported {construct} was omitted from arrangement '{structure}'."));
    }
}
