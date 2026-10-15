// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Defines the requested treatment of implementation gaps. Support is target-defined.
/// </summary>
public enum ImplementationGapMode
{
    /// <summary>
    /// Refuse requirements that cannot be implemented. This is the default.
    /// </summary>
    Refuse = 0,

    /// <summary>
    /// Request typed stubs for supported gaps; this does not authorize publication.
    /// </summary>
    Stub = 1
}
