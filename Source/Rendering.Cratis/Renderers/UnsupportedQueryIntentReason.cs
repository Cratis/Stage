// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Identifies authored query intent the legacy renderer cannot preserve.
/// </summary>
public enum UnsupportedQueryIntentReason
{
    /// <summary>
    /// The query declares a filter parameter contract.
    /// </summary>
    FilterContract,

    /// <summary>
    /// The query delegates its behavior to a file.
    /// </summary>
    FilePerformer,

    /// <summary>
    /// The query delegates its behavior to inline code.
    /// </summary>
    InlinePerformer,

    /// <summary>
    /// The performer has neither a file nor code, or ambiguously has both.
    /// </summary>
    UnsupportedPerformer
}
