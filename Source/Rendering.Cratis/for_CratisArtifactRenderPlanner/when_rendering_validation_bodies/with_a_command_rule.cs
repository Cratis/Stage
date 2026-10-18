// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_command_rule : Specification
{
    ArtifactRenderPlan _plan = null!;
    Assembly _assembly = null!;

    void Because()
    {
        _plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command("return context.Value > 0 && context.Artifact.Amount == context.Value && context.Property == \"amount\" && !context.IsWholeArtifact;", generated: true)));
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        _assembly = opaque_policy_model.Compile(_plan);
    }

    [Fact] void should_accept_a_positive_value() => validation_body_model.Validate(_assembly, "PlaceOrder", 1m).IsValid.ShouldBeTrue();
    [Fact] void should_reject_a_nonpositive_value_with_the_authored_message() => validation_body_model.Validate(_assembly, "PlaceOrder", 0m).Errors.Single().ErrorMessage.ShouldEqual("Positive amount required");
    [Fact] void should_have_no_screenplay_runtime_dependency() => _assembly.GetReferencedAssemblies().ShouldNotContain(reference => reference.Name!.StartsWith("Cratis.Screenplay", StringComparison.Ordinal));
}
#endif
