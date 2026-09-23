// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The property names every layer that builds or reads a Scene element for this Stage agrees on - the
/// converters that write them, and the host that resolves routes from them.
/// </summary>
/// <remarks>
/// Declared once, here, rather than by each writer separately. A table element and a synthesized command form
/// are produced by different converters for different reasons, but a route is attached to both by the same
/// pass looking for the same property name - a converter using its own name for "the same thing" would render
/// correctly and simply never receive a route, silently, which is exactly the failure this constant exists to
/// rule out.
/// </remarks>
public static class SceneElementProperties
{
    /// <summary>
    /// The property an element carries the modeled artifact's emitted type name in - the read model a table or
    /// summary shows, or the command a form submits.
    /// </summary>
    public const string TypeName = "typeName";

    /// <summary>
    /// The property the host writes the resolved API route into.
    /// </summary>
    public const string Route = "route";

    /// <summary>
    /// The property a command element carries its JSON schema in, so a frontend can build its form.
    /// </summary>
    public const string Schema = "schema";
}
