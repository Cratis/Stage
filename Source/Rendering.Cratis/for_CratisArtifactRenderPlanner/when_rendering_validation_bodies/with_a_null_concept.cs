// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using FluentValidation;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_null_concept
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_reject_the_null_root_before_executing_the_validation_body(bool iterator)
    {
        var source = iterator
            ? validation_body_model.IteratorConcept("yield return \"Body executed\";")
            : validation_body_model.Concept("throw new InvalidOperationException(\"Body executed\");");
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        var conceptType = assembly.GetTypes().Single(type => type.Name == "Amount");
        var validator = (IValidator)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == "AmountValidator"))!;
        var context = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(conceptType), [null])!;

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(context));

        Assert.Equal("Cannot pass a null model to Validate/ValidateAsync. The root model must be non-null.", exception.Message);
    }
}
#endif
