// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_lowering_root_projection_operations : Specification
{
    [Theory]
    [InlineData("add visits by quantity", "AddFromAttribute<")]
    [InlineData("subtract visits by quantity", "SubtractFromAttribute<")]
    public void should_lower_direct_numeric_contributions(string mapping, string attribute)
    {
        var source = when_selecting_model_bound_projections.Source.Replace("title String", "quantity Int", StringComparison.Ordinal)
            .Replace("name = title", mapping, StringComparison.Ordinal).Replace("decrement visits", string.Empty, StringComparison.Ordinal);
        var plan = when_selecting_model_bound_projections.Plan(source, "model-bound");
        var code = when_selecting_model_bound_projections.Code(plan);
        Assert.DoesNotContain("IProjectionFor", code);
        Assert.Contains(attribute, code);
        Assert.Contains("ProjectChanged.Quantity)", code);
        Assert.Empty(RenderedOutput.Errors(when_selecting_model_bound_projections.Files(plan)));
    }

    [Theory]
    [InlineData("String", "\"fixed\"")]
    [InlineData("Int", "42")]
    [InlineData("Bool", "true")]
    public void should_lower_attribute_constant_values(string type, string value)
    {
        var source = when_selecting_model_bound_projections.Source.Replace("String", type, StringComparison.Ordinal)
            .Replace("name = title", $"name = {value}", StringComparison.Ordinal);
        var plan = when_selecting_model_bound_projections.Plan(source, "model-bound");
        var code = when_selecting_model_bound_projections.Code(plan);
        Assert.DoesNotContain("IProjectionFor", code);
        Assert.Contains("SetValueAttribute<", code);
        Assert.Contains($">({value})", code);
        Assert.Empty(RenderedOutput.Errors(when_selecting_model_bound_projections.Files(plan)));
    }
}
#endif
