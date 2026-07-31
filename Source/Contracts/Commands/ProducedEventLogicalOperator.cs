// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Defines the operators combining two conditions.
/// </summary>
public enum ProducedEventLogicalOperator
{
    /// <summary>
    /// Both conditions must hold.
    /// </summary>
    And = 0,

    /// <summary>
    /// At least one of the conditions must hold.
    /// </summary>
    Or = 1
}
