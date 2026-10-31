// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_nested_anonymous_policies : Specification
{
    [Theory]
    [InlineData("not")]
    [InlineData("condition")]
    [InlineData("authorization")]
    public void should_refuse_the_operation_instead_of_granting_access(string form)
    {
        var model = SemanticModelSerializer.Deserialize(AnonymousPoliciesCorpus.V31.EsmBytes.AsSpan());
        var execution = SemanticExecutionPlan.Compile(model).Plan;
        var slices = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices);
        var command = slices.SelectMany(slice => slice.Commands).First();
        var authorization = command.Authorization;
        var policy = model.Application.Policies.Single(policy => policy.Name == ((SemanticPolicyReference)authorization).Name);
        if (form == "authorization")
        {
            typeof(SemanticCommand).GetProperty(nameof(SemanticCommand.Authorization)).SetValue(command,
                new SemanticLogicalAuthorization(authorization, SemanticLogicalOperator.And, authorization));
        }
        else
        {
            SemanticPolicyCondition condition = form == "not"
                ? new SemanticNotPolicyCondition(policy.Condition)
                : new SemanticLogicalPolicyCondition(policy.Condition, SemanticLogicalOperator.Or, new SemanticAuthenticatedCondition());
            typeof(SemanticPolicy).GetProperty(nameof(SemanticPolicy.Condition)).SetValue(policy, condition);
        }

        var plan = CratisRendering.Plan(model, execution, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("AnonymousApp", "Anonymous"));
        plan.Success.ShouldBeFalse();
        plan.CanPublishArtifacts.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-015" && diagnostic.Artifact == command.Id);
    }
}
