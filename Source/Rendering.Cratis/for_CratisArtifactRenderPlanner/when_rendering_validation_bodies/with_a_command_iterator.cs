// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_command_iterator : Specification
{
    Assembly _assembly = null!;

    void Because()
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.IteratorCommand("""
            if (context.Artifact.Amount <= 0) yield return "Positive amount required";
            if (context.Value.Amount <= 0 && context.IsWholeArtifact && context.Property == "") yield return "Whole command rejected";
            """)));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Fact] void should_accept_a_command_without_yielded_messages() => validation_body_model.Validate(_assembly, "PlaceOrder", 1m, DateTimeOffset.UnixEpoch).IsValid.ShouldBeTrue();
    [Fact] void should_reject_with_each_yielded_message_in_order() => Assert.Equal(["Positive amount required", "Whole command rejected"], validation_body_model.Validate(_assembly, "PlaceOrder", 0m, DateTimeOffset.UnixEpoch).Errors.Select(error => error.ErrorMessage));
    [Fact] void should_keep_the_blocking_floor() => _assembly.GetTypes().Single(type => type.Name == "PlaceOrder").CustomAttributes.ShouldContain(attribute => attribute.AttributeType.Name == "BlockOnValidationSeverityAttribute");
}
#endif
