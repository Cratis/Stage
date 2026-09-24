// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Semantics;

/// <summary>
/// Exposes whether a semantic runtime can still serve its in-process world.
/// </summary>
public interface ISemanticRuntimeStatus
{
    /// <summary>
    /// Gets the reason this runtime can no longer safely evaluate its world, if faulted.
    /// </summary>
    string? FaultReason { get; }
}
