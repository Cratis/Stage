// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// What came of answering an invocation.
/// </summary>
public enum InvocationAnswer
{
    /// <summary>
    /// The invocation took the answer.
    /// </summary>
    Answered = 0,

    /// <summary>
    /// No such invocation exists.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// The invocation does not wait for an answer.
    /// </summary>
    NotWaiting = 2,

    /// <summary>
    /// The identity that answered does not satisfy the persona or policy of the invocation.
    /// </summary>
    Forbidden = 3,

    /// <summary>
    /// The outlet is not one the ability declares.
    /// </summary>
    UnknownOutlet = 4
}
