// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Screenplay.DesignSystems;
using Cratis.Scene.Screenplay.Forward;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay application into the Scene application Stage renders - the one place the live Stage and the
/// generated application get their Scene from, so the two cannot disagree about what the model says.
/// </summary>
/// <remarks>
/// <para>
/// The conversion is <c language="csharp">Cratis.Scene.Screenplay</c>'s: <see cref="ScreenplaySceneVisitor"/> converts screens (declared in a slice
/// or free and adopted), templates, layouts, profiles, forms, navigation and sample data, and <see cref="DesignSystemConverter"/> resolves the
/// design system the application declares. This adds the one step that joins them: every ui profile that draws from that design system
/// carries it as <see cref="Cratis.Scene.Model.Profiles.UiProfile.DesignSystem"/>, so what the selected theme and colour scheme resolve to
/// reaches the rendered application as the design system's anatomy rather than as a hand-carried accent (Cratis/Stage#1081).
/// </para>
/// <para>
/// An adopted free screen is one Scene screen owned by the first slice that adopts it, its needs bound to what that slice provides; a free screen no slice adopts
/// is left out, as it never reaches the executable model.
/// </para>
/// <para>
/// A profile draws from the design system when it selects a theme that implements it, or selects none (the design system's default theme
/// applies). A profile that selects a theme the design system does not know keeps the theme it names and no design system, which the plan
/// reports as <see cref="RenderFindingKind.ThemeNotFound"/> where the application declares no such theme at all.
/// </para>
/// </remarks>
public static class StageSceneConversion
{
    /// <summary>
    /// Converts a compiled application into the Scene application Stage renders, refusing a construct Scene cannot say.
    /// </summary>
    /// <param name="application">The compiled Screenplay application.</param>
    /// <returns>The converted <see cref="SceneApplication"/>, its profiles carrying the design system.</returns>
    /// <exception cref="UnsupportedUiSyntax">A construct the application uses cannot be converted faithfully.</exception>
    public static SceneApplication Visit(ApplicationSyntax application) =>
        WithDesignSystem(new ScreenplaySceneVisitor().Visit(application), application, null);

    /// <summary>
    /// Converts a compiled application into the Scene application Stage renders, reporting what Scene cannot say rather than refusing it.
    /// </summary>
    /// <param name="application">The compiled Screenplay application.</param>
    /// <returns>The converted <see cref="SceneApplication"/>, its profiles carrying the design system and its <see cref="SceneApplication.ConversionDiagnostics"/> everything left out.</returns>
    public static SceneApplication Convert(ApplicationSyntax application)
    {
        var scene = new ScreenplaySceneVisitor().Convert(application);
        var diagnostics = new global::Cratis.Scene.Screenplay.ConversionDiagnostics();
        var converted = WithDesignSystem(scene, application, diagnostics);

        return converted with { ConversionDiagnostics = [.. converted.ConversionDiagnostics, .. diagnostics.All] };
    }

    static SceneApplication WithDesignSystem(SceneApplication scene, ApplicationSyntax application, global::Cratis.Scene.Screenplay.ConversionDiagnostics? diagnostics)
    {
        // A free screen no slice adopts is design-only: it never reaches the executable model, so it is not rendered either (Screenplay decision 0166).
        // The bridge owns an adopted free screen by its first adopter and leaves an unadopted one without an owner, which is what tells them apart here.
        scene = scene with { Screens = [.. scene.Screens.Where(screen => screen.Owner is not null)] };
        if (DesignSystemConverter.Convert(application, null, diagnostics) is not { } designSystem)
        {
            return scene;
        }

        var themes = designSystem.Themes.Select(theme => theme.Name).ToHashSet(StringComparer.Ordinal);

        return scene with
        {
            UiProfiles = [.. scene.UiProfiles.Select(profile => profile.Theme is null || themes.Contains(profile.Theme) ? profile with { DesignSystem = designSystem } : profile)]
        };
    }
}
