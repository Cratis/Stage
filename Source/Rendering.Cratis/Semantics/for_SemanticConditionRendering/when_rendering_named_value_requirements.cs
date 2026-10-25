// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticConditionRendering;

public class when_rendering_named_value_requirements
{
    [Theory]
    [InlineData("value == inReview", "object.Equals(command.Value, Status.InReview)")]
    [InlineData("value != \"draft\"", "!object.Equals(command.Value, Status.Draft)")]
    [InlineData("value == other", "object.Equals(command.Value, command.Other)")]
    [InlineData("value != other", "!object.Equals(command.Value, command.Other)")]
    [InlineData("value == inReview and value != draft", "&&")]
    [InlineData("value == inReview or value != draft", "||")]
    public void should_compare_enum_members_and_same_concept_properties(string condition, string expected)
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
                    value Status
                    other Status
                    validate
                      require {{condition}}
                    returns id
            """);
        var plan = invoice_model.Plan(model);
        plan.Success.ShouldBeTrue();
        var code = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Check.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        code.ShouldContain(expected);
        code.ShouldNotContain("command.Value.Value");
        code.ShouldNotContain("command.Other.Value");
    }
}
