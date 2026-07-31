// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Defines where the value of a produced event's property comes from.
/// </summary>
public enum ProducedValueKind
{
    /// <summary>
    /// The value is copied from a property on the command payload, named by the expression.
    /// </summary>
    CommandProperty = 0,

    /// <summary>
    /// The value is a constant, held in the expression as JSON text.
    /// </summary>
    Literal = 1,

    /// <summary>
    /// The value is the time the event occurred.
    /// </summary>
    Occurred = 2,

    /// <summary>
    /// The value comes from the identity that caused the command, at the path held in the expression.
    /// </summary>
    Identity = 3,

    /// <summary>
    /// The value is the environment variable named by the expression.
    /// </summary>
    Environment = 4,

    /// <summary>
    /// The value is an interpolated string, where <c>${property}</c> placeholders resolve against the command payload.
    /// </summary>
    Template = 5,

    /// <summary>
    /// The modeled expression has no runtime equivalent, so the property is left off the event payload.
    /// </summary>
    Unsupported = 6
}
