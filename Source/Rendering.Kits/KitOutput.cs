// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>An output template with opaque expansion expressions owned by the rendering engine.</summary>
public sealed record KitOutput(string Id, string Template, string Path)
{
    /// <summary>Gets the optional opaque iteration expression.</summary>
    public string? ForEach { get; init; }

    /// <summary>Gets the optional opaque condition expression.</summary>
    public string? When { get; init; }
}
