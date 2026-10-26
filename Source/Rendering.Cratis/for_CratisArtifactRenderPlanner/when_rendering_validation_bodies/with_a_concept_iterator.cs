// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_concept_iterator : Specification
{
    Assembly _assembly = null!;

    void Because()
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.IteratorConcept("""
            if (context.Value <= 0 && context.IsWholeArtifact) yield return "Positive amount required";
            if (context.Artifact < 0) yield return "Negative amount";
            """)));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Fact] void should_accept_a_concept_without_yielded_messages() => validation_body_model.Validate(_assembly, "Amount", 1m).IsValid.ShouldBeTrue();
    [Fact] void should_reject_with_each_yielded_message_in_order() => Assert.Equal(["Positive amount required", "Negative amount"], validation_body_model.Validate(_assembly, "Amount", -1m).Errors.Select(error => error.ErrorMessage));
}
#endif
