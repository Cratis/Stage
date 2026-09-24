// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Provides the ESM policy verdict to Arc's command authorization filter for this scenario only.
/// </summary>
/// <param name="plan">The execution plan.</param>
/// <param name="command">The command declaration.</param>
/// <param name="specification">The current specification.</param>
/// <param name="principal">The caller principal.</param>
/// <param name="commandType">The generated command type.</param>
internal sealed class SemanticArcAuthorization(SemanticExecutionPlan plan, SemanticCommand command, SemanticSpecification specification, ClaimsPrincipal principal, Type commandType) : IAuthorizationEvaluator
{
    /// <inheritdoc/>
    public bool IsAuthorized(Type type) => type != commandType || SemanticPolicyEvaluator.Allows(command.Authorization, plan, specification.GivenCaller, principal, command, specification);

    /// <inheritdoc/>
    public bool IsAuthorized(MethodInfo method) => true;
}
