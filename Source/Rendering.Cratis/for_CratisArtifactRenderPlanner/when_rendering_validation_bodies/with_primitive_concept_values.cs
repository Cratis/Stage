// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_primitive_concept_values
{
    [Theory]
    [InlineData("string", "", "Required reason", false)]
    [InlineData("string", "", "Required reason", true)]
    [InlineData("int", 0, 1, false)]
    [InlineData("int", 0, 1, true)]
    public void should_compile_and_execute_validation_with_the_underlying_primitive(string type, object invalid, object valid, bool iterator)
    {
        var primitive = type == "string" ? "String" : "Int";
        var condition = type == "string" ? "reason.Length > 0" : "reason > 0";
        var body = $"{type} reason = context.Value; {type} artifact = context.Artifact; " +
            (iterator ? $"if (!({condition}) && artifact == reason) yield return \"A reason is required\";" : $"return {condition} && artifact == reason;");
        var validation = iterator ? string.Empty : "rule Required message \"A reason is required\"";
        var source = $$"""
            concept Reason : {{primitive}}
              validate
                {{validation}}
                  ```csharp
                  {{body}}
                  ```
            module Decisions
              feature Recording
                slice StateChange Record
                  command Record
                    recordId Uuid identifier
                    reason Reason
                    produces Recorded
                      for recordId
                      reason = reason
                  event Recorded
                    reason Reason
            """;
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Contains(opaque_policy_model.Files(plan), file => file.Content.Contains($"{type} Value", StringComparison.Ordinal) && file.Content.Contains($"{type} Artifact", StringComparison.Ordinal));
        var assembly = opaque_policy_model.Compile(plan);
        Assert.True(validation_body_model.Validate(assembly, "Reason", valid).IsValid);
        Assert.Equal("A reason is required", validation_body_model.Validate(assembly, "Reason", invalid).Errors.Single().ErrorMessage);
    }
}
#endif
