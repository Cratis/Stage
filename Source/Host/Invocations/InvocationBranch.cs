// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Names the branch an origin takes for a settled invocation: <c lang="text">returned</c>, the named outcome, or <c lang="text">failed</c>.
/// These are the clauses of a reaction (<c lang="text">on returned</c>, <c lang="text">on &lt;Outcome&gt;</c>, <c lang="text">on failed</c>) and the outlets of a workflow action.
/// </summary>
public static class InvocationBranch
{
    /// <summary>
    /// Gets the branch.
    /// </summary>
    /// <param name="record">The settled invocation.</param>
    /// <returns>The branch name.</returns>
    public static string Of(InvocationRecord record) =>
        record.Status == InvocationStatus.Failed || record.Outcome.Length == 0 ? AbilityDescriptor.FailedOutlet : record.Outcome;
}
