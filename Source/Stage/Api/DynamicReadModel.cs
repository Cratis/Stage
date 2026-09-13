// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage.Api;

/// <summary>
/// Base type for the runtime read model types the engine emits per modeled read model. A distinct runtime type
/// per read model keeps query performers uniquely identifiable to Arc even though the read models have no
/// compile-time shape.
/// </summary>
public class DynamicReadModel
{
    /// <summary>
    /// Gets the identity of the read model instance.
    /// </summary>
    /// <remarks>
    /// Every document the modeled projections build carries one, and it is what a by-id query matches on. It is
    /// declared here rather than emitted per type because it is the one property every read model has whatever
    /// its model says.
    /// </remarks>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the modeled properties of the document.
    /// </summary>
    /// <remarks>
    /// A modeled read model has no compile-time shape, so its properties cannot be declared here and emitting
    /// them per type would fix the shape at the moment the type is emitted. Extension data lets the document the
    /// projection actually built serialize as the read model's own properties - which is what a played screen shows.
    /// </remarks>
    [JsonExtensionData]
    public IDictionary<string, JsonElement> Values { get; init; } = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
}
