// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs;

public class with_an_optional_body_input : Specification
{
    const string Source = """
        concept Moment : DateTime
        module Dates
          feature Windows
            slice StateChange Keep
              command Keep
                id String identifier
                at Moment optional
                amount Decimal
                validate
                  at >= "2026-01-01T00:00:00Z" message "Too early."
                  amount rule Positive message "Invalid amount or year."
                    ```csharp
                    return (context.Artifact.At is null || context.Artifact.At.Value.Year >= 2026) && context.Value > 0;
                    ```
                returns id
        """;

    Assembly _assembly;

    void Establish()
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(Source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Theory]
    [InlineData("", 1, true)]
    [InlineData(",\"at\":null", 1, true)]
    [InlineData(",\"at\":\"2026-10-17T00:00:00Z\"", 1, true)]
    [InlineData(",\"at\":\"2026-10-17T00:00:00Z\"", 0, false)]
    void should_validate_a_body_against_the_nullable_nominal_artifact(string assignment, int amount, bool valid)
    {
        var command = datetime_concept_model.Bind(_assembly, $$"""{"id":"one","amount":{{amount}}{{assignment}}}""");
        datetime_concept_model.Validate(_assembly, command).IsValid.ShouldEqual(valid);
    }
}
#endif
