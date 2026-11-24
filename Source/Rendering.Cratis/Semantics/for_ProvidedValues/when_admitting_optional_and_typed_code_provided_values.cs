// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_admitting_optional_and_typed_code_provided_values : Specification
{
    [Theory]
    [InlineData("String optional", "return null;", "string?")]
    [InlineData("String optional", "return \"present\";", "string?")]
    [InlineData("Int optional", "return null;", "long?")]
    [InlineData("Int optional", "return 14L;", "long?")]
    [InlineData("Bool optional", "return null;", "bool?")]
    [InlineData("Bool optional", "return true;", "bool?")]
    [InlineData("Uuid optional", "return null;", "Guid?")]
    [InlineData("Uuid optional", "return global::System.Guid.Empty;", "Guid?")]
    void should_admit_optional_values_and_emit_exactly_one_nullable_marker(string type, string body, string expected)
    {
        var plan = a_code_provided_model.Plan(body, type);
        a_code_provided_model.AssertSuccess(plan);
        var artifact = plan.Artifacts.Single(item => item.RelativePath.EndsWith("ProvidedValues.cs", StringComparison.Ordinal));
        var source = Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
        Assert.Contains($"public static {expected} Provide(", source, StringComparison.Ordinal);
        Assert.DoesNotContain($"{expected}?", source, StringComparison.Ordinal);
    }

    [Fact]
    void should_admit_the_actual_command_shape_used_by_arguments_as()
    {
        const string body = "return context.ArgumentsAs<global::Fleet.Fleet.Vessels.RegisterVessel.RegisterVessel>().Name;";
        var plan = a_code_provided_model.Plan(body);
        a_code_provided_model.AssertSuccess(plan);
        var artifact = plan.Artifacts.Single(item => item.RelativePath.EndsWith("ProvidedValues.cs", StringComparison.Ordinal));
        Assert.Contains(body, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), StringComparison.Ordinal);
    }
}
#endif
