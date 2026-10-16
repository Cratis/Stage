// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Shell;
using Cratis.Scene.Engine.Templates;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using ScenePackagesModel = Cratis.Scene.Model.Packages;
using SceneProfilesModel = Cratis.Scene.Model.Profiles;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Asks the Scene engine what a ui profile's <c language="csharp">root</c> template resolves to - the same
/// <see cref="ShellResolver"/> the TypeScript <c language="csharp">resolveEffectiveShell</c> mirrors and the Stage's browser runtime calls
/// (REN2: one rule set, not a Stage copy).
/// </summary>
/// <remarks>
/// The .NET side knows the application's own layouts and templates in full, but a blueprint only as its package manifest: the
/// names of the layouts and templates it ships and its version, not their structure. The library therefore holds a blueprint
/// layout as a shell with the one <c language="csharp">content</c> outlet every shell must have, and a blueprint screen template as
/// one that may take the root role. That is enough for what is decidable at build time - the blueprint is installed, the pin
/// is satisfied by the installed version, the named shell exists - and never rejects a shell the browser could still draw. What
/// needs the real structure (outlets, the root role of a blueprint screen template) is checked by the runtime, with the full
/// blueprint, against the same engine.
/// </remarks>
public static class RootShell
{
    /// <summary>
    /// Resolves the effective shell of a profile.
    /// </summary>
    /// <param name="profile">The profile, packages already expanded to the closure.</param>
    /// <param name="application">The application whose layouts and templates are the profile's own.</param>
    /// <param name="catalog">The packages available, among them the installed blueprints.</param>
    /// <returns>The effective shell, with the engine's diagnostics.</returns>
    public static EffectiveShell Resolve(
        SceneProfilesModel.UiProfile profile,
        SceneApplication application,
        IReadOnlyList<ScenePackagesModel.ScenePackage> catalog)
    {
        var own = new TemplateSource(
            new ScenePackagesModel.ScenePackage("Application", "1.0.0", ScenePackagesModel.PackageKind.ComponentLibrary, [], [], [], [], [], []),
            application.Layouts,
            application.ScreenTemplates,
            application.DialogTemplates);

        var blueprints = catalog
            .Where(package => package.Kind == ScenePackagesModel.PackageKind.Blueprint)
            .Select(package => new TemplateSource(
                package,
                [.. package.Layouts.Select(name => new SceneLayouts.Layout(name, [new SceneLayouts.Slot("content")]))],
                [.. package.ScreenTemplates.Select(name => new SceneScreens.ScreenTemplate(name, null, [new SceneLayouts.Slot("content")]) { Metadata = new SceneScreens.TemplateMetadata(Role: SceneScreens.TemplateRole.Root) })],
                [.. package.DialogTemplates.Select(name => new SceneScreens.DialogTemplate(name, [new SceneLayouts.Slot("content")]))]))
            .ToList();

        return ShellResolver.Resolve(
            profile,
            new ShellResolutionContext(new TemplateLibrary(own, blueprints), Themes: application.Themes, AvailablePackages: [.. catalog.Select(package => package.Name)]));
    }
}
