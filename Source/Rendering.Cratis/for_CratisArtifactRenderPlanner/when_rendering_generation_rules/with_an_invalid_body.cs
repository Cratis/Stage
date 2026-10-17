// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class with_an_invalid_body
{
    [Theory]
    [InlineData("return value;", "STAGE-ESM-019")]
    [InlineData("return 42;", "STAGE-ESM-019")]
    [InlineData("var missing = 42;", "STAGE-ESM-019")]
    [InlineData("return context.Occurred.ToString();", "STAGE-ESM-019")]
    [InlineData("return \"ok\"; } static string Escaped() { return \"bad\";", "STAGE-ESM-019")]
    [InlineData("string? missing = null; return missing;", "STAGE-ESM-019")]
    [InlineData("var unused = 42; return \"ok\";", "STAGE-ESM-019")]
    [InlineData("#if true\nreturn \"ok\";\n#endif", "STAGE-ESM-022")]
    public void should_refuse_the_body_without_emitting_artifacts(string body, string code)
    {
        var plan = opaque_policy_model.Plan(generation_rule_model.Load(body));
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Fact]
    public void should_locate_compile_errors_at_the_authored_fence()
    {
        var loaded = generation_rule_model.Load("return value;");
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var plan = opaque_policy_model.Plan(loaded);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" &&
            diagnostic.Message.Contains($"{requirement.Source.Span.Document}:{requirement.BodyLines[0].Line}:", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("CS0103", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Guid")]
    [InlineData("GenerateValue")]
    public void should_refuse_a_namespace_that_rebinds_a_body_type(string name)
    {
        var loaded = opaque_policy_model.Load(generation_rule_model.Source(name == "Guid" ? generation_rule_model.PortableBody : generation_rule_model.TargetBody)
            .Replace("module Shop", $"module {name}", StringComparison.Ordinal));
        var plan = opaque_policy_model.Plan(loaded);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022");
    }
}
#endif
