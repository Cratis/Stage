// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class without_an_authored_message : Specification
{
    Assembly _assembly = null!;

    void Because()
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command("return false;", message: string.Empty, generated: true)));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Fact] void should_preserve_the_reference_fallback_message() => validation_body_model.Validate(_assembly, "PlaceOrder", 1m).Errors.Single().ErrorMessage.ShouldEqual("A value does not satisfy the 'RulePredicate' rule.");
}
#endif
