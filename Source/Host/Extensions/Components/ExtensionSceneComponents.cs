// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.Extensions.Packages;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneInteractions = Cratis.Scene.Model.Interactions;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Turns the components a screen places through a system into the <c language="csharp">Stage:extensionComponent</c> elements the Stage runtime loads.
/// </summary>
/// <remarks>
/// The model names <c language="csharp">Mailer.MailPreview</c>, never a package. This is where the system becomes a locked package: the element gets the address of
/// the package's frontend, the abilities the contract lets the component invoke and the signals it raises, so the browser asks for nothing the
/// contract does not allow. A signal a screen handles with <c language="csharp">on &lt;signal&gt;</c> is bound here: the model writes it as an application
/// trigger, and where the name is a signal the component's contract declares it becomes Scene's signal trigger, so the runtime runs the interaction when the
/// component raises that signal. A name the contract does not declare stays the application trigger it is. See
/// <see href="https://github.com/Cratis/Stage/blob/main/Documentation/extensions/components.md">the extension components documentation</see>.
/// </remarks>
public static class ExtensionSceneComponents
{
    /// <summary>
    /// The name of the element the Stage runtime renders an extension component with.
    /// </summary>
    public const string ElementName = "Stage:extensionComponent";

    static readonly string[] _placed = ["name", "context", "icon", "presentation", "exposes"];

    /// <summary>
    /// Rewrites the extension components of every screen of the scene.
    /// </summary>
    /// <param name="scene">The <see cref="SceneApplication"/>.</param>
    /// <param name="components">The <see cref="IExtensionComponentCatalog"/> of the model.</param>
    /// <param name="packages">The <see cref="IExtensionPackages"/>.</param>
    /// <returns>The scene whose extension components can be loaded.</returns>
    public static SceneApplication Apply(SceneApplication scene, IExtensionComponentCatalog components, IExtensionPackages packages) =>
        components.All.Count == 0 ? scene : scene with { Screens = [.. scene.Screens.Select(screen => Apply(screen, components, packages))] };

    static SceneScreens.Screen Apply(SceneScreens.Screen screen, IExtensionComponentCatalog components, IExtensionPackages packages) =>
        screen with
        {
            SlotContent = screen.SlotContent.ToDictionary(
                slot => slot.Key,
                slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(element => Rewrite(screen.Name, element, components, packages))],
                StringComparer.Ordinal)
        };

    static SceneElements.SceneElement Rewrite(string screen, SceneElements.SceneElement element, IExtensionComponentCatalog components, IExtensionPackages packages)
    {
        if (element is not SceneElements.ExternalComponent component) return element;

        var slots = component.Slots.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(child => Rewrite(screen, child, components, packages))],
            StringComparer.Ordinal);
        var dot = component.ComponentName.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || components.Find(screen, component.ComponentName[..dot], component.ComponentName[(dot + 1)..]) is not { } placement)
        {
            return component with { Slots = slots };
        }

        var package = packages.ForSystem(placement.System);
        return component with
        {
            ComponentName = ElementName,
            Slots = slots,
            Behaviors = [.. component.Behaviors.Select(behavior => BindSignals(behavior, placement.Signals))],
            Properties = Properties(screen, component, placement, package)
        };
    }

    static SceneInteractions.Behavior BindSignals(SceneInteractions.Behavior behavior, IReadOnlyList<string> signals) =>
        behavior with
        {
            Bindings = [.. behavior.Bindings.Select(binding => binding.Trigger is SceneInteractions.ApplicationTriggerInteractionTrigger application && signals.Contains(application.TriggerName, StringComparer.Ordinal)
                ? binding with { Trigger = new SceneInteractions.SignalInteractionTrigger(application.TriggerName) }
                : binding)]
        };

    static Dictionary<string, object?> Properties(string screen, SceneElements.ExternalComponent component, ExtensionComponentPlacement placement, LoadedExtensionPackage? package)
    {
        var declared = component.Properties.Where(property => !_placed.Contains(property.Key)).ToDictionary(property => property.Key, property => property.Value, StringComparer.Ordinal);
        var properties = new Dictionary<string, object?>(component.Properties, StringComparer.Ordinal)
        {
            ["screen"] = screen,
            ["system"] = placement.System,
            ["component"] = placement.Component,
            ["kind"] = placement.Kind.ToString().ToLowerInvariant(),
            ["invokes"] = placement.Invokes,
            ["signals"] = placement.Signals,
            ["properties"] = declared,
            ["strings"] = new Dictionary<string, string>()
        };
        foreach (var name in declared.Keys) properties.Remove(name);

        if (package?.Contract.Frontend is { } frontend)
        {
            var entry = frontend.StartsWith("./", StringComparison.Ordinal) ? frontend[2..] : frontend;
            properties["module"] = $"{ExtensionPackageEndpoints.AssetsRoute.TrimStart('/')}/{package.Id}/{entry[(entry.LastIndexOf('/') + 1)..]}";
        }

        if (package?.Contract.Components.FirstOrDefault(candidate => candidate.Name == placement.Component) is { Placeholder.Length: > 0 })
        {
            properties["placeholder"] = $"{ExtensionPackageDescription.Route}/packages/{package.Id}/placeholders/{placement.Component}";
        }

        return properties;
    }
}
