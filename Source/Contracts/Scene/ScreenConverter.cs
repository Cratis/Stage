// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneContributionPoints = Cratis.Scene.Model.ContributionPoints;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneForms = Cratis.Scene.Model.Forms;
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
/// A screen is an instance: it names the structure it fills and provides the content. A
/// <c>template &lt;Name&gt;</c> directive (<see cref="ScreenplaySyntax.ScreenTemplateReferenceSyntax"/>) names a
/// screen or dialog template and fills its slots, and becomes
/// <see cref="SceneScreens.Screen.ScreenTemplate"/> plus the slot-keyed
/// <see cref="SceneScreens.Screen.SlotContent"/>.
/// </para>
/// <para>
/// A screen <em>never</em> names the application's <c>layout</c> - the shell is selected once per build by a
/// <c>ui profile</c>, which is what keeps a screen portable across web, mobile and desktop. Scene's
/// <see cref="SceneScreens.Screen.Layout"/> is nevertheless a required, resolved name, so the caller resolves
/// it from the application's declared shell and passes it in.
/// </para>
/// <para>
/// A Level-1 "intent" screen (per <c>screens.md</c>) names no template at all - <c>data</c>/<c>action</c> sit
/// directly under <c>screen</c> and Studio generates the component. That is precisely the case
/// <see cref="SceneScreens.Screen.ScreenTemplate"/> documents as <see langword="null"/>: the screen fills the
/// layout's own slots directly. So nothing is synthesized for it - it keeps a <see langword="null"/> template
/// and its content fills <see cref="DefaultLayout.ContentSlotName"/>. Synthesizing a screen template instead
/// would claim the screen fills a reusable, <c>fits slot</c>-placed shape, which is exactly what a Level-1
/// screen does not do, and would still leave <see cref="SceneScreens.Screen.Layout"/> to resolve. The same
/// applies to a <c>file</c>-referenced screen, whose content lives entirely outside Screenplay - only a single
/// <c>core:file</c> element is produced, carrying the referenced path.
/// </para>
/// <para>
/// <see cref="SceneScreens.Screen.Forms"/> is resolved per <see cref="ScreenplaySyntax.FormSyntax"/>'s own
/// doc comment: a form is discovered by its <c>For</c> command binding wherever that command is invoked,
/// never nested in a screen's own directive tree. This walks every <see cref="ScreenplaySyntax.ScreenActionSyntax.Command"/>
/// referenced anywhere in the screen (including inside <c>section</c>s and template slots) and includes every
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
    /// <param name="layoutName">The resolved name of the application layout the screen renders inside.</param>
    /// <param name="availableForms">Every form declared in the screen's enclosing module.</param>
    /// <param name="contributions">The already-converted contributions for the screen's enclosing scope.</param>
    /// <returns>The converted <see cref="SceneScreens.Screen"/>.</returns>
    /// <exception cref="UnsupportedScreenContent">Thrown when the screen mixes a top-level <c>template</c> directive with other top-level directives.</exception>
    public static SceneScreens.Screen Convert(
        ScreenplaySyntax.ScreenSyntax screen,
        string layoutName,
        IReadOnlyList<ScreenplaySyntax.FormSyntax> availableForms,
        IReadOnlyList<SceneContributionPoints.Contribution> contributions)
    {
        var (screenTemplate, slotContent) = ConvertContent(screen);
        var forms = ResolveForms(screen, availableForms);

        return new SceneScreens.Screen(screen.Name, layoutName, slotContent, forms, [.. contributions], screenTemplate);
    }

    static (string? ScreenTemplate, IReadOnlyDictionary<string, IReadOnlyList<SceneElements.SceneElement>> SlotContent) ConvertContent(
        ScreenplaySyntax.ScreenSyntax screen)
    {
        if (screen.File is not null)
        {
            return (null, ContentSlot([SceneElementFactory.Component($"{screen.Name}.file", "core:file", new Dictionary<string, object?> { ["path"] = screen.File.Path })]));
        }

        var templateReferences = screen.Directives.OfType<ScreenplaySyntax.ScreenTemplateReferenceSyntax>().ToList();
        if (templateReferences.Count > 1 || (templateReferences.Count == 1 && screen.Directives.Count() > 1))
        {
            throw new UnsupportedScreenContent(screen.Name);
        }

        if (templateReferences.Count == 1)
        {
            var templateReference = templateReferences[0];
            var slotContent = templateReference.Slots.ToDictionary(
                slot => slot.Name,
                slot => ScreenDirectiveConverter.Convert(slot.Directives, $"{screen.Name}.{slot.Name}"),
                StringComparer.Ordinal);

            return (templateReference.Name, slotContent);
        }

        return (null, ContentSlot(ScreenDirectiveConverter.Convert(screen.Directives, screen.Name)));
    }

    static Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> ContentSlot(IReadOnlyList<SceneElements.SceneElement> content) =>
        new(StringComparer.Ordinal) { [DefaultLayout.ContentSlotName] = content };

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
            ScreenplaySyntax.ScreenTemplateReferenceSyntax template => template.Slots.SelectMany(slot => CommandsReferencedBy(slot.Directives)),
            ScreenplaySyntax.ScreenSlotSyntax slot => CommandsReferencedBy(slot.Directives),
            _ => [],
        });
}
