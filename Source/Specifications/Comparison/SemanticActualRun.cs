// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Comparison;

/// <summary>
/// What an executed specification actually appended, for comparison with its expectations.
/// </summary>
/// <param name="Facts">The facts appended after the given events.</param>
/// <param name="Destinations">The destination of each fact.</param>
/// <param name="Plan">The execution plan, when the comparison depends on the model's version.</param>
/// <param name="ActionFacts">The number of facts the specification's action appended, when known.</param>
internal sealed record SemanticActualRun(
    IReadOnlyList<SemanticSpecificationEvent> Facts,
    IReadOnlyList<SemanticValue> Destinations,
    SemanticExecutionPlan? Plan = null,
    int? ActionFacts = null)
{
    /// <summary>
    /// Gets every invocation the specification's slice's reactions made, in invocation order.
    /// </summary>
    internal IReadOnlyList<SemanticRecordedInvocation> Invocations { get; init; } = [];
}
