// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents the kinds of global using directive.
/// </summary>
internal enum CatalogUsingKind
{
    /// <summary>
    /// A <c language="csharp">global using N;</c> directive importing the types of a namespace.
    /// </summary>
    Namespace = 0,

    /// <summary>
    /// A <c language="csharp">global using static T;</c> directive importing the static members and nested types of a type.
    /// </summary>
    Static = 1,

    /// <summary>
    /// A <c language="csharp">global using A = T;</c> directive.
    /// </summary>
    Alias = 2
}
