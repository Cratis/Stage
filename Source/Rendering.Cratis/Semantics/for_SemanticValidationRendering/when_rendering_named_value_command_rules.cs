// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering;

public class when_rendering_named_value_command_rules
{
    [Theory]
    [InlineData("==", "")]
    [InlineData("!=", "")]
    [InlineData("==", " optional")]
    [InlineData("!=", " optional")]
    public void should_compare_enum_members_without_primitive_unwrapping(string comparison, string optional)
    {
        var model = invoice_model.Compile($$"""
            concept Status : Enum
              draft = -7
              inReview = 42
            module Inputs
              feature NamedValues
                slice StateChange Check
                  command Check
                    id String
                    value Status{{optional}}
                    validate
                      value {{comparison}} inReview
                    returns id
            """);
        var plan = invoice_model.Plan(model);
        plan.Success.ShouldBeTrue();
        var code = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Check.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        code.ShouldContain($"Must(value => {(optional.Length > 0 ? "value is null || " : string.Empty)}value {comparison} Status.InReview)");
        code.ShouldNotContain("value.Value");
        code.ShouldNotContain("object.Equals(value, \"inReview\")");
    }
}
