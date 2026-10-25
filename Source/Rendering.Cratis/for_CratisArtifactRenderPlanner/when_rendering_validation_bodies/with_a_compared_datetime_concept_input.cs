// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_compared_datetime_concept_input : Specification
{
    [Theory]
    [InlineData("return context.Artifact.At.Year >= 2026 && context.Value > 0;")]
    [InlineData("return context.Artifact.Amount > 0;")]
    void should_compile_and_execute_against_the_runtime_command_input_shape(string body)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(Source(body)));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        validation_body_model.Validate(assembly, "Book", "booking", new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero), 1m).IsValid.ShouldBeTrue();
        validation_body_model.Validate(assembly, "Book", "booking", new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero), 0m).IsValid.ShouldBeFalse();
    }

    [Fact]
    void should_refuse_concept_member_access_on_the_raw_bound_input()
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(Source("return context.Artifact.At.Value.Year >= 2026;")));
        rendering_refusals.Rejected(plan, "STAGE-ESM-019");
    }

    static string Source(string body) => $$"""
        concept Moment : DateTime
        module Scheduling
          feature Windows
            slice StateChange Book
              command Book
                id String identifier
                at Moment
                amount Decimal
                validate
                  at >= "2026-01-01T00:00:00Z" message "Too early."
                  amount rule Positive message "Positive amount required"
                    ```csharp
                    {{body}}
                    ```
                produces Booked
                  for id
                  at = at
                  amount = amount
              event Booked
                at Moment
                amount Decimal
        """;
}
#endif
