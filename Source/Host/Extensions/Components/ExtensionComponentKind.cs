// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// How an extension's frontend component is hosted, which is the kind of the extension component contract.
/// </summary>
public enum ExtensionComponentKind
{
    /// <summary>
    /// A React component rendered in Stage's own React tree, sharing React with it.
    /// </summary>
    React = 0,

    /// <summary>
    /// A component given a host element, which is how Vue, Svelte, Lit, web components and plain DOM take part.
    /// </summary>
    Mount = 1,

    /// <summary>
    /// A component authored in Screenplay templates in the extension's package.
    /// </summary>
    Screenplay = 2
}
