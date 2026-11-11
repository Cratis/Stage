// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// What came of reconciling an invocation.
/// </summary>
public enum InvocationReconciliation
{
    /// <summary>
    /// The outcome was recorded and the origin continues from it.
    /// </summary>
    Reconciled = 0,

    /// <summary>
    /// No such invocation exists.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// The invocation is not stopped for reconciliation.
    /// </summary>
    NotStopped = 2,

    /// <summary>
    /// The outcome is not one the ability declares.
    /// </summary>
    UnknownOutcome = 3
}
