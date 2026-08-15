// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// What a slice renderer emits, so <see cref="UnrenderedConstructs"/> can report everything the slice declares
/// beyond it. A renderer is registered per slice type, but a slice may declare constructs its type's
/// renderer knows nothing about — a <c>command</c> on an Automation slice, a <c>projection</c> on a State Change
/// slice — and those went nowhere and said nothing.
/// </summary>
[Flags]
public enum RenderedConstructs
{
    /// <summary>
    /// The renderer emits the slice's events and nothing else.
    /// </summary>
    None = 0,

    /// <summary>
    /// The renderer emits the slice's first command.
    /// </summary>
    Command = 1 << 0,

    /// <summary>
    /// The renderer emits a read model for the slice's projection.
    /// </summary>
    ReadModel = 1 << 1,

    /// <summary>
    /// The renderer emits the slice's reactions.
    /// </summary>
    Reactions = 1 << 2,
}
