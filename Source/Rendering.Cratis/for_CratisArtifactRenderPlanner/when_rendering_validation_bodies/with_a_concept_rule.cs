// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_concept_rule : Specification
{
    Assembly _assembly = null!;

    void Because()
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Concept("return context.Value.Value > 0 && context.IsWholeArtifact;")));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Fact] void should_accept_a_positive_concept() => validation_body_model.Validate(_assembly, "Amount", 1m).IsValid.ShouldBeTrue();
    [Fact] void should_reject_a_nonpositive_concept() => validation_body_model.Validate(_assembly, "Amount", 0m).Errors.Single().ErrorMessage.ShouldEqual("Positive amount required");
}
#endif
