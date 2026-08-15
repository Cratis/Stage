// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneContributionPoints = Cratis.Scene.Model.ContributionPoints;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneForms = Cratis.Scene.Model.Forms;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneScreens = Cratis.Scene.Model.Screens;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.ScreenSyntax"/> into a
/// <see cref="SceneScreens.Screen"/> - part of Cratis/Stage#37, the most involved conversion in the
/// translation seam.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SceneScreens.Screen.Layout"/> is a required, non-null name, but a Level-1 "intent" screen
/// (per <c>screens.md</c>) has no <c>layout</c> directive at all - <c>data</c>/<c>action</c> sit directly
/// under <c>screen</c>. For that shape, this converter synthesizes an implicit one-slot layout (named
/// <c>"&lt;ScreenName&gt;.implicit"</c>, one slot named <c>"content"</c>) and returns it alongside the
/// screen so the caller adds it to the layout set. The same happens for a <c>file</c>-referenced screen,
/// whose content lives entirely outside Screenplay - only a single <c>core:file</c> element is produced,
/// carrying the referenced path.
/// </para>
/// <para>
/// <see cref="SceneScreens.Screen.Forms"/> is resolved per <see cref="ScreenplaySyntax.FormSyntax"/>'s own
/// doc comment: a form is discovered by its <c>For</c> command binding wherever that command is invoked,
/// never nested in a screen's own directive tree. This walks every <see cref="ScreenplaySyntax.ScreenActionSyntax.Command"/>
/// referenced anywhere in the screen (including inside <c>section</c>s and layout slots) and includes every
/// module-level form whose <c>For</c> matches one of them.
/// </para>
/// <para>
/// <see cref="SceneScreens.Screen.Contributions"/> has no source anywhere in
/// <see cref="ScreenplaySyntax.ScreenSyntax"/>/<see cref="ScreenplaySyntax.ScreenDirectiveSyntax"/> -
/// <c>contribute to</c> only ever appears on a module or feature. The caller supplies the already-converted
/// contributions for this screen's enclosing scope.
/// </para>
/// </remarks>
public static class ScreenConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.ScreenSyntax"/> into a <see cref="SceneScreens.Screen"/>.
    /// </summary>
    /// <param name="screen">The <see cref="ScreenplaySyntax.ScreenSyntax"/> to convert.</param>
    /// <param name="availableForms">Every form declared in the screen's enclosing module.</param>
    /// <param name="contributions">The already-converted contributions for the screen's enclosing scope.</param>
    /// <returns>The <see cref="ScreenConversionResult"/>.</returns>
    /// <exception cref="UnsupportedScreenContent">Thrown when the screen mixes a top-level <c>layout</c> directive with other top-level directives.</exception>
    public static ScreenConversionResult Convert(
        ScreenplaySyntax.ScreenSyntax screen,
        IReadOnlyList<ScreenplaySyntax.FormSyntax> availableForms,
        IReadOnlyList<SceneContributionPoints.Contribution> contributions)
    {
        var (layoutName, slotContent, implicitLayout) = ConvertContent(screen);
        var forms = ResolveForms(screen, availableForms);

        var sceneScreen = new SceneScreens.Screen(screen.Name, layoutName, slotContent, forms, [.. contributions]);
        return new ScreenConversionResult(sceneScreen, implicitLayout);
    }

    static (string LayoutName, IReadOnlyDictionary<string, IReadOnlyList<SceneElements.SceneElement>> SlotContent, SceneLayouts.Layout? ImplicitLayout) ConvertContent(
        ScreenplaySyntax.ScreenSyntax screen)
    {
        if (screen.File is not null)
        {
            return ImplicitSingleSlot(screen.Name, [SceneElementFactory.Component($"{screen.Name}.file", "core:file", new Dictionary<string, object?> { ["path"] = screen.File.Path })]);
        }

        var layoutDirectives = screen.Directives.OfType<ScreenplaySyntax.ScreenLayoutSyntax>().ToList();
        if (layoutDirectives.Count > 1 || (layoutDirectives.Count == 1 && screen.Directives.Count() > 1))
        {
            throw new UnsupportedScreenContent(screen.Name);
        }

        if (layoutDirectives.Count == 1)
        {
            var layoutDirective = layoutDirectives[0];
            var slotContent = layoutDirective.Slots.ToDictionary(
                slot => slot.Name,
                slot => ScreenDirectiveConverter.Convert(slot.Directives, $"{screen.Name}.{slot.Name}"),
                StringComparer.Ordinal);

            return (layoutDirective.Name, slotContent, null);
        }

        return ImplicitSingleSlot(screen.Name, ScreenDirectiveConverter.Convert(screen.Directives, screen.Name));
    }

    static (string, IReadOnlyDictionary<string, IReadOnlyList<SceneElements.SceneElement>>, SceneLayouts.Layout) ImplicitSingleSlot(
        string screenName, IReadOnlyList<SceneElements.SceneElement> content)
    {
        const string slotName = "content";
        var layoutName = $"{screenName}.implicit";
        var slotContent = new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { [slotName] = content };
        var implicitLayout = new SceneLayouts.Layout(layoutName, [new SceneLayouts.Slot(slotName)]);

        return (layoutName, slotContent, implicitLayout);
    }

    static IReadOnlyList<SceneForms.Form> ResolveForms(ScreenplaySyntax.ScreenSyntax screen, IReadOnlyList<ScreenplaySyntax.FormSyntax> availableForms)
    {
        var commands = new HashSet<string>(CommandsReferencedBy(screen.Directives), StringComparer.Ordinal);
        return [.. availableForms.Where(form => commands.Contains(form.For)).Select(FormConverter.Convert)];
    }

    static IEnumerable<string> CommandsReferencedBy(IEnumerable<ScreenplaySyntax.ScreenDirectiveSyntax> directives) =>
        directives.SelectMany(directive => directive switch
        {
            ScreenplaySyntax.ScreenActionSyntax action => [action.Command],
            ScreenplaySyntax.ScreenSectionSyntax section => CommandsReferencedBy(section.Directives),
            ScreenplaySyntax.ScreenLayoutSyntax layout => layout.Slots.SelectMany(slot => CommandsReferencedBy(slot.Directives)),
            ScreenplaySyntax.ScreenSlotSyntax slot => CommandsReferencedBy(slot.Directives),
            _ => [],
        });
}
