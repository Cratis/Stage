// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Identifies an unsupported shape in the bounded root string key profile.
/// </summary>
public enum UnsupportedRootStringKeyReason
{
    /// <summary>
    /// A literal is empty.
    /// </summary>
    EmptyLiteral,

    /// <summary>
    /// A literal cannot be preserved by the target value expression grammar.
    /// </summary>
    UnencodableLiteral,

    /// <summary>
    /// An effective subscription does not use a string literal.
    /// </summary>
    MixedKeys,

    /// <summary>
    /// The projection declares a separate identifying key.
    /// </summary>
    ExplicitIdentity,

    /// <summary>
    /// The inferred root record has a conventional Id property.
    /// </summary>
    ConventionalId,

    /// <summary>
    /// An affected root subscription declares a parent key.
    /// </summary>
    RootParent,

    /// <summary>
    /// An owned query declares an identifying parameter other than required primitive String.
    /// </summary>
    IncompatibleByType,

    /// <summary>
    /// The inferred root record has no properties for the target to apply its class-level constant key.
    /// </summary>
    PropertylessRecord
}
