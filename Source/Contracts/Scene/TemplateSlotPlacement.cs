// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneElements = Cratis.Scene.Model.Elements;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneOutlets = Cratis.Scene.Model.Outlets;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Places what a screen template declares for its slots into each screen that fills the template: the outlets it draws in a slot
/// (<c language="screenplay">outlet filters of filters in header</c>), the routed slots the screen leaves empty, and the slot a
/// <c language="screenplay">show &lt;Screen&gt; by &lt;param&gt; from &lt;element&gt;.selection</c> line stands in.
/// </summary>
/// <remarks>
/// <para>
/// Scene draws an outlet where a <c language="csharp">core:outletItems</c> element places it and a slot where a <c language="csharp">core:slot</c>
/// element stands, and a screen's own content replaces a template's seed for a slot. So what the template declares has to stand in the
/// screen's content: an outlet with a slot becomes a <c language="csharp">core:outletItems</c> element at the start of that slot, unless the
/// screen already places an outlet by that name (its toolbar). Outlets of navigation are drawn by the shell and are left alone.
/// </para>
/// <para>
/// A routed slot (<c language="screenplay">detail pane routed by sku optional</c>) the screen does not fill becomes a <c language="csharp">core:slot</c>
/// element with the slot's name, role, routing and optionality, so a hook that opens a screen in it, and the address that names it, have a
/// place to show it. A slot an outlet of tabs owns is filled by those tabs, and a panel is never routed, so neither gets one.
/// </para>
/// <para>
/// The slot a <c language="screenplay">show</c> line stands in (<see cref="ScreenDirectiveConverter.ShowsProperty"/>) takes its name, role and
/// routing from the template's slot it is written in, and the id of the element whose selection it shows.
/// </para>
/// </remarks>
public static class TemplateSlotPlacement
{
    /// <summary>
    /// The property that names the slot or outlet a <c language="csharp">core:slot</c> or <c language="csharp">core:outletItems</c> element draws.
    /// </summary>
    public const string NameProperty = "name";

    /// <summary>
    /// Places what each screen's template declares for its slots into the screen.
    /// </summary>
    /// <param name="screens">The converted screens.</param>
    /// <param name="templates">The converted screen templates of the application.</param>
    /// <returns>The screens, with the outlets and slots their templates declare placed in them.</returns>
    public static IReadOnlyList<SceneScreens.Screen> Place(IEnumerable<SceneScreens.Screen> screens, IReadOnlyList<SceneScreens.ScreenTemplate> templates) =>
        [.. screens.Select(screen => Place(screen, templates))];

    /// <summary>
    /// Places what a screen's template declares for its slots into the screen.
    /// </summary>
    /// <param name="screen">The converted screen.</param>
    /// <param name="templates">The converted screen templates of the application.</param>
    /// <returns>The screen, with the outlets and slots its template declares placed in it; unchanged when it fills no template the application declares.</returns>
    public static SceneScreens.Screen Place(SceneScreens.Screen screen, IReadOnlyList<SceneScreens.ScreenTemplate> templates)
    {
        var template = screen.ScreenTemplate is { } name ? templates.FirstOrDefault(candidate => candidate.Name == name) : null;
        if (template is null) return screen;

        var elements = screen.SlotContent.Values.SelectMany(slot => slot.SelectMany(Flatten)).OfType<SceneElements.ExternalComponent>().ToList();
        var placedOutlets = elements
            .Where(element => element.ComponentName == ScreenDirectiveConverter.OutletItemsComponent)
            .Select(element => Text(element, NameProperty))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        var outlets = template.Outlets ?? [];
        var content = screen.SlotContent.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(element => Completed(element, template.Slots.FirstOrDefault(candidate => candidate.Name == slot.Key), slot.Key, elements))],
            StringComparer.Ordinal);

        foreach (var placed in outlets
            .Where(outlet => outlet.Slot is not null && outlet.Kind != SceneOutlets.OutletKind.Navigation && !placedOutlets.Contains(outlet.Name))
            .GroupBy(outlet => outlet.Slot!, StringComparer.Ordinal))
        {
            content[placed.Key] = [.. placed.Select(outlet => OutletElement(screen.Name, outlet)), .. content.GetValueOrDefault(placed.Key) ?? []];
        }

        foreach (var slot in template.Slots.Where(slot =>
            slot.Routed == true &&
            slot.Role != SceneLayouts.SlotRole.Panel &&
            !screen.SlotContent.ContainsKey(slot.Name) &&
            !outlets.Any(outlet => outlet.Kind == SceneOutlets.OutletKind.Tabs && outlet.Slot == slot.Name)))
        {
            content[slot.Name] = [.. content.GetValueOrDefault(slot.Name) ?? [], SceneElementFactory.Component($"{screen.Name}.{slot.Name}.slot", ScreenDirectiveConverter.SlotComponent, SlotProperties(slot, slot.Name))];
        }

        return screen with { SlotContent = content };
    }

    /// <summary>
    /// The properties a <c language="csharp">core:slot</c> element draws a template's slot with: its name, role, routing and optionality.
    /// </summary>
    /// <param name="slot">The template's slot, or null when the template does not declare it.</param>
    /// <param name="name">The slot's name.</param>
    /// <returns>The properties, leaving out what the slot does not state.</returns>
    public static IReadOnlyDictionary<string, object?> SlotProperties(SceneLayouts.Slot? slot, string name)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal) { [NameProperty] = name };
        if (slot?.Role is { } role) properties["role"] = role.ToString().ToLowerInvariant();
        if (slot?.Routed == true) properties["routed"] = true;
        if (slot?.RoutedBy is { Count: > 0 } routedBy) properties["routedBy"] = routedBy.ToList();
        if (slot?.Optional == true) properties["optional"] = true;
        return properties;
    }

    static SceneElements.SceneElement Completed(SceneElements.SceneElement element, SceneLayouts.Slot? slot, string name, IReadOnlyList<SceneElements.ExternalComponent> elements)
    {
        if (element is not SceneElements.ExternalComponent component ||
            component.ComponentName != ScreenDirectiveConverter.SlotComponent ||
            Text(component, ScreenDirectiveConverter.ShowsProperty) is null)
        {
            return element;
        }

        var properties = new Dictionary<string, object?>(component.Properties, StringComparer.Ordinal);
        foreach (var (key, value) in SlotProperties(slot, name)) properties[key] = value;
        if (Text(component, ScreenDirectiveConverter.ShowsFromProperty) is { } from && SelectionSource(from, elements) is { } source)
        {
            properties[ScreenDirectiveConverter.ShowsFromElementProperty] = source.Id;
        }

        return component with { Properties = properties };
    }

    static SceneElements.ExternalComponent? SelectionSource(string name, IReadOnlyList<SceneElements.ExternalComponent> elements) =>
        elements.FirstOrDefault(element => element.ComponentName != ScreenDirectiveConverter.SlotComponent && Text(element, SceneElementProperties.TypeName) == name) ??
        elements.FirstOrDefault(element => element.ComponentName != ScreenDirectiveConverter.SlotComponent && Text(element, NameProperty) == name);

    static SceneElements.ExternalComponent OutletElement(string screen, SceneOutlets.Outlet outlet) =>
        SceneElementFactory.Component($"{screen}.{outlet.Slot}.outlet-{outlet.Name}", ScreenDirectiveConverter.OutletItemsComponent, new Dictionary<string, object?>
        {
            [NameProperty] = outlet.Name,
        });

    static string? Text(SceneElements.ExternalComponent element, string property) =>
        element.Properties.TryGetValue(property, out var value) && value is string text && text.Length > 0 ? text : null;

    static IEnumerable<SceneElements.SceneElement> Flatten(SceneElements.SceneElement element) =>
        element is SceneElements.ExternalComponent component
            ? [element, .. component.Slots.Values.SelectMany(children => children.SelectMany(Flatten))]
            : [element];
}
