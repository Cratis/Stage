// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// Structured symbol use kinds.
/// </summary>
public enum TemplateSymbolKind
{
    /// <summary>
    /// A symbol declaration.
    /// </summary>
    Declaration,

    /// <summary>
    /// A symbol reference.
    /// </summary>
    Reference,

    /// <summary>
    /// An imports insertion point.
    /// </summary>
    Imports
}
