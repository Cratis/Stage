// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_designated_string : Specification
{
    Assembly _assembly = null!;

    void Because()
    {
        const string body = "return context.Value is string text && text.StartsWith(\"ORD-\");";
        var source = validation_body_model.Command(body, generated: true).Replace("amount Decimal", "amount String", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Contains(opaque_policy_model.Files(plan), file => file.Content.Contains(body, StringComparison.Ordinal));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Fact] public void should_invoke_the_audited_method_on_the_designated_string() => validation_body_model.Validate(_assembly, "PlaceOrder", "ORD-1").IsValid.ShouldBeTrue();
    [Fact] public void should_reject_a_string_without_the_required_prefix() => validation_body_model.Validate(_assembly, "PlaceOrder", "other").IsValid.ShouldBeFalse();
}
#endif
