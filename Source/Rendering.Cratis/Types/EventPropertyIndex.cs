// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Types;

/// <summary>
/// Indexes the declared properties of every event across an <see cref="ApplicationSet"/> — the lookup a
/// projection needs to type the read-model property a mapping produces, and to tell whether a
/// <c>nameof(TEvent.Property)</c> reference will actually bind.
/// </summary>
public sealed class EventPropertyIndex
{
    readonly Dictionary<string, Dictionary<string, TypeRefSyntax>> _events;

    EventPropertyIndex(Dictionary<string, Dictionary<string, TypeRefSyntax>> events) => _events = events;

    /// <summary>
    /// Builds the index for every event declared across the applications.
    /// </summary>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to index.</param>
    /// <returns>The <see cref="EventPropertyIndex"/>.</returns>
    public static EventPropertyIndex Build(ApplicationSet applicationSet)
    {
        var events = new Dictionary<string, Dictionary<string, TypeRefSyntax>>(StringComparer.Ordinal);

        foreach (var @event in applicationSet.Slices.SelectMany(slice => slice.Slice.Events))
        {
            var properties = new Dictionary<string, TypeRefSyntax>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in @event.Properties)
            {
                properties[property.Name] = property.Type;
            }

            events[@event.Name] = properties;
        }

        return new EventPropertyIndex(events);
    }

    /// <summary>
    /// Gets whether an event is declared anywhere in the applications.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns><see langword="true"/> when the event is declared.</returns>
    public bool Declares(string eventName) => _events.ContainsKey(eventName);

    /// <summary>
    /// Gets whether an event declares a property.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns><see langword="true"/> when the event declares the property.</returns>
    public bool Declares(string eventName, string propertyName) =>
        _events.TryGetValue(eventName, out var properties) && properties.ContainsKey(propertyName);

    /// <summary>
    /// Gets the declared type of an event property.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The <see cref="TypeRefSyntax"/>, or <see langword="null"/> when the property is not declared.</returns>
    public TypeRefSyntax? TypeOf(string eventName, string propertyName) =>
        _events.TryGetValue(eventName, out var properties) && properties.TryGetValue(propertyName, out var type) ? type : null;
}
