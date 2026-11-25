// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// The kind of screen element that can carry an explicit availability gate.
/// </summary>
public enum ScreenElementKind
{
    /// <summary>An action bound to a command.</summary>
    Action,

    /// <summary>A data element bound to a query.</summary>
    Data
}
