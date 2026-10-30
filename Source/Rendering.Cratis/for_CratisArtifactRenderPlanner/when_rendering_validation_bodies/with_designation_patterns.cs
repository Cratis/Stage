// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_designation_patterns : Specification
{
    Assembly _numbers = null!;

    void Because()
    {
        _numbers = Compile("Int", "return context.Value is long n && n > 0;");
    }

    [Fact] public void should_accept_a_positive_designated_number() => validation_body_model.Validate(_numbers, "PlaceOrder", 1L).IsValid.ShouldBeTrue();
    [Fact] public void should_reject_a_nonpositive_designated_number() => validation_body_model.Validate(_numbers, "PlaceOrder", 0L).IsValid.ShouldBeFalse();

    static Assembly Compile(string type, string body)
    {
        var source = validation_body_model.Command(body, generated: true).Replace("amount Decimal", $"amount {type}", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.LoadWholeNumbers(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Contains(opaque_policy_model.Files(plan), file => file.Content.Contains(body, StringComparison.Ordinal));
        return opaque_policy_model.Compile(plan);
    }
}
#endif
