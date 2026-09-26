// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_v3;

public class with_opaque_policy_authorization : a_command_only_plan
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_match_reference_ordered_short_circuit_outcome(bool opaqueFirst, bool or)
    {
        var command = _plan.Commands.Values.Single();
        var authorization = new SemanticLogicalAuthorization(
            new SemanticPolicyReference(opaqueFirst ? "CustomAccess" : "ManagersOnly"),
            or ? SemanticLogicalOperator.Or : SemanticLogicalOperator.And,
            new SemanticPolicyReference(opaqueFirst ? "ManagersOnly" : "CustomAccess"));
        var protectedCommand = command with { Authorization = authorization };
        var specification = _specification with { GivenCaller = new SemanticCaller(true, [], []), ThenEvents = [], ThenDenied = true };
        var model = _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice with
            {
                Commands = [.. slice.Commands.Select(value => value.Id == command.Id ? protectedCommand : value)],
                Specifications = [.. slice.Specifications.Select(value => value.Id == specification.Id ? specification : value with { GivenCaller = new SemanticCaller(true, [], []) })]
            })] }] }],
            Policies = [.. model.Application.Policies,
                new SemanticPolicy("ManagersOnly", new SemanticRoleCondition("Manager")),
                new SemanticPolicy("CustomAccess", new SemanticOpaquePolicyCondition("policy-body"))]
        };
        var plan = SemanticExecutionPlan.Compile(CreateV3(application)).Plan!;
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        var unsupported = opaqueFirst || or;
        Assert.Equal(unsupported, reference.Execution is SemanticUnsupported { Capability: SemanticExecutionCapability.Authorization });
        Assert.Equal(unsupported ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed, result.Outcome);
        if (unsupported)
        {
            Assert.Equal(StageExecutionCapability.Authorization, result.Unsupported?.Capability);
            Assert.Contains("CustomAccess", result.Unsupported!.Details);
        }
    }
}
