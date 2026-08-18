// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Screens;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <c>screen</c> declarations of a slice into Stage <see cref="ScreenDefinition"/>
/// records - the read models a screen shows and the commands it offers.
/// </summary>
/// <remarks>
/// Named for what it converts rather than for the construct, because it deliberately converts only part of one.
/// A screen's presentation - its templates, slots, arrangements, widgets and theming - is translated in whole
/// by <see cref="Scene.ScreenConverter"/> into <see cref="Scene.SceneApplication"/>, a parallel output of the
/// same source. What that translation has no place for is the screen's standing in the event model, which is
/// what this carries.
/// <para>
/// Directives are collected through sections, slots and template references rather than off the top level
/// only, so how deeply a screen nests its content changes where the content appears and never whether it is
/// carried.
/// </para>
/// </remarks>
public static class ScreenBindingConverter
{
    /// <summary>
    /// Converts a slice's screen declarations into their Stage records.
    /// </summary>
    /// <param name="screens">The screen declarations.</param>
    /// <param name="slicePath">The fully-qualified slice path, used to derive stable identifiers.</param>
    /// <returns>The Stage screen definitions, in declaration order.</returns>
    public static IReadOnlyList<ScreenDefinition> Convert(IEnumerable<ScreenSyntax> screens, string slicePath) =>
    [
        .. screens.Select(screen =>
        {
            var directives = Flatten(screen.Directives).ToArray();

            return new ScreenDefinition(
                DeterministicId.From($"{slicePath}.screen.{screen.Name}"),
                screen.Name,
                screen.File?.Path ?? string.Empty,
                [.. directives.OfType<ScreenDataSyntax>().Select(data => new ScreenDataBinding(data.Type.Name, data.Query, data.By))],
                [.. directives.OfType<ScreenActionSyntax>().Select(action => new ScreenAction(action.Command, action.Label, action.Navigate?.Screen))]);
        })
    ];

    static IEnumerable<ScreenDirectiveSyntax> Flatten(IEnumerable<ScreenDirectiveSyntax> directives) =>
        directives.SelectMany(directive => directive switch
        {
            ScreenSectionSyntax section => [directive, .. Flatten(section.Directives)],
            ScreenSlotSyntax slot => [directive, .. Flatten(slot.Directives)],
            ScreenTemplateReferenceSyntax template => [directive, .. Flatten(template.Slots)],
            _ => (IEnumerable<ScreenDirectiveSyntax>)[directive]
        });
}
