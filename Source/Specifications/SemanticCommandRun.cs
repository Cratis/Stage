// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications;

/// <summary>
/// A command about to be executed for a specification: the scenario state, the materialized command and who it runs as.
/// </summary>
/// <param name="Context">The scenario state of the command.</param>
/// <param name="Instance">The materialized runtime command.</param>
/// <param name="Slice">The slice owning the specification.</param>
/// <param name="Expected">The specification whose result is reported.</param>
/// <param name="Plan">The model's execution plan.</param>
/// <param name="ReactionPrincipal">The principal a reaction invokes the command as; <see langword="null"/> for the specification's own action.</param>
internal sealed record SemanticCommandRun(
    SemanticRunContext Context,
    DynamicCommand Instance,
    SemanticSlice Slice,
    SemanticSpecification Expected,
    SemanticExecutionPlan Plan,
    ClaimsPrincipal? ReactionPrincipal = null)
{
    /// <summary>
    /// Gets whether a reaction, rather than the specification's own action, invokes the command.
    /// </summary>
    internal bool IsInvocation => ReactionPrincipal is not null;
}
