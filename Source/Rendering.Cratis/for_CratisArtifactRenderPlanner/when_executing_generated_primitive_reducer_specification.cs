// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_primitive_reducer_specification : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        const string originalBody = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
        var source = when_rendering_a_pure_reducer.Source
            .Replace("        amount Decimal\n        produces", "        amount Decimal\n        note String\n        produces", StringComparison.Ordinal)
            .Replace("          amount = amount\n      event", "          amount = amount\n          note = note\n      event", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal", "      event OrderPlaced\n        id Uuid\n        amount Decimal\n        note String\n      specification WithPrimitives\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n          note = \"shipped\"\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n          note = \"shipped\"\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n          note = \"shipped\"", StringComparison.Ordinal)
            .Replace("      readmodel Total\n        id Uuid\n        amount Decimal", "      readmodel Total\n        id Uuid\n        amount Decimal\n        note String", StringComparison.Ordinal);
        var loaded = when_rendering_a_pure_reducer.Load(source.Replace(originalBody, "return context.State;", StringComparison.Ordinal)).GetAwaiter().GetResult();
        var readModel = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.ReadModels.Length > 0).ReadModels.Single();
        var expressions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "context.Event.Id",
            ["amount"] = "context.Event.Amount",
            ["note"] = "context.Event.Note"
        };
        var arguments = string.Join(", ", SemanticStateViewArtifactRenderer.OrderedProperties(readModel.Properties).Select(property => expressions[property.Name]));
        var plan = when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(source.Replace(originalBody, $"return new Total({arguments});", StringComparison.Ordinal)).GetAwaiter().GetResult());
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.StartsWith("Common/", StringComparison.Ordinal));
        var spec = plan.Artifacts.Single(artifact => artifact.RelativePath.Contains("with_primitives_is_projected", StringComparison.Ordinal));
        Assert.DoesNotContain("using Projects.Common;", System.Text.Encoding.UTF8.GetString(spec.Bytes.AsSpan()), StringComparison.Ordinal);
        return plan;
    }

    [Fact]
    async Task should_build_and_pass_the_guid_text_decimal_expectations_without_common()
    {
        try
        {
            // The required keyed query's primitive Guid argument triggers Arc's unrelated ARC0015 analyzer.
            var build = await Run("primitive-reducer-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "-p:NoWarn=ARC0015%3BCS7022", "--nologo");
            BuildWarnings(build).ShouldEqual(string.Empty);
            var test = await Run("primitive-reducer-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", test, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
