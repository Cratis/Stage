// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents what a simple name binds to.
/// </summary>
internal enum NameLookup
{
    /// <summary>
    /// Nothing in scope has the name.
    /// </summary>
    Nothing = 0,

    /// <summary>
    /// The name binds to exactly the intended type.
    /// </summary>
    Target = 1,

    /// <summary>
    /// The name binds to something else, or is ambiguous.
    /// </summary>
    Other = 2,

    /// <summary>
    /// A namespace in scope has types the resolver does not know, so the binding cannot be proven.
    /// </summary>
    Unknown = 3
}
