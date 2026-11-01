// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_nested_anonymous_policies : Specification
{
    [Theory]
    [InlineData("not")]
    [InlineData("condition")]
    [InlineData("authorization")]
    public void should_refuse_the_invalid_model_and_specification(string form)
    {
        var model = SemanticModelSerializer.Deserialize(AnonymousPoliciesCorpus.V31.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var specification = plan.Specifications.Values.First(specification => specification.When is not null);
        var command = plan.Commands[specification.When.Command];
        var authorization = command.Authorization;
        var policy = model.Application.Policies.Single(policy => policy.Name == ((SemanticPolicyReference)authorization).Name);
        if (form == "authorization")
        {
            typeof(SemanticCommand).GetProperty(nameof(SemanticCommand.Authorization)).SetValue(command,
                new SemanticLogicalAuthorization(authorization, SemanticLogicalOperator.Or, authorization));
        }
        else
        {
            SemanticPolicyCondition condition = form == "not" ? new SemanticNotPolicyCondition(policy.Condition)
                : new SemanticLogicalPolicyCondition(policy.Condition, SemanticLogicalOperator.And, new SemanticAuthenticatedCondition());
            typeof(SemanticPolicy).GetProperty(nameof(SemanticPolicy.Condition)).SetValue(policy, condition);
        }
        SemanticRunAdmission.ModelFeatures(model).ShouldContain(feature => feature.Artifact == command.Id.ToString() && feature.Details.StartsWith("STAGE-ESM-015:", StringComparison.Ordinal));
        SemanticRunAdmission.Check(plan, specification).Details.ShouldContain("STAGE-ESM-015");
    }
}
