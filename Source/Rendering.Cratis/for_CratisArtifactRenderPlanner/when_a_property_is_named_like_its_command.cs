// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_property_is_named_like_its_command : Specification
{
    ArtifactRenderPlan _plan;

    void Because() => _plan = member_collision_model.Plan("Text", "ById");

    [Fact] void should_render_without_errors() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_report_the_exact_escape() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Information && diagnostic.Message.Contains("Command 'Text' property 'text'", StringComparison.Ordinal) && diagnostic.Message.Contains("Text_", StringComparison.Ordinal));
    [Fact]
    void should_compile_and_serialize_the_escaped_wire_field()
    {
        Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
        var assembly = RenderedOutput.Load(member_collision_model.Files(_plan));
        var type = assembly.GetType("Payments.Payments.Recording.RecordPayment.Text")!;
        var instance = Activator.CreateInstance(type, "payment-1", "paid");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(instance, options);
        Assert.Equal("paid", JsonDocument.Parse(json).RootElement.GetProperty("text_").GetString());
        Assert.Equal(instance, JsonSerializer.Deserialize(json, type, options));
        Assert.Equal("paid", type.GetMethod("Handle")!.Invoke(instance, null)!.GetType().GetProperty("Text")!.GetValue(type.GetMethod("Handle")!.Invoke(instance, null)));
    }
}
#endif
