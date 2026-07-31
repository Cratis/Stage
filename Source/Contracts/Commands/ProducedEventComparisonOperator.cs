// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Defines the comparisons a <see cref="ProducedEventComparison"/> can apply.
/// </summary>
public enum ProducedEventComparisonOperator
{
    /// <summary>
    /// The values must be equal.
    /// </summary>
    Equal = 0,

    /// <summary>
    /// The values must not be equal.
    /// </summary>
    NotEqual = 1,

    /// <summary>
    /// The command value must be greater than the constant.
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// The command value must be greater than or equal to the constant.
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// The command value must be less than the constant.
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// The command value must be less than or equal to the constant.
    /// </summary>
    LessThanOrEqual = 5
}
