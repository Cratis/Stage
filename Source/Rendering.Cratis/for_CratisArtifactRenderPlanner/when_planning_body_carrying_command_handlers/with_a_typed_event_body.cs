// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

public class with_a_typed_event_body
{
    [Fact]
    public void should_render_the_verified_body_in_a_separate_typed_helper()
    {
        var plan = handler_model.Plan(handler_model.Request());
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var source = string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        Assert.Contains(handler_model.Body, source, StringComparison.Ordinal);
        Assert.Contains("HandlerBody_", source, StringComparison.Ordinal);
        Assert.Contains("HandlerCommand_", source, StringComparison.Ordinal);
        Assert.DoesNotContain("object[]", source, StringComparison.Ordinal);
    }

    [Fact]
    public void should_render_receipt_occurrence_on_the_fast_handler_path()
    {
        var plan = handler_model.Plan(handler_model.Request(handler_model.TimedBody, handler_model.TimedSource));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var source = string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        Assert.Contains("Handle(IOperationContextAccessor operation)", source, StringComparison.Ordinal);
        Assert.Contains("CommandReceiptTime.OccurredAtReceipt(operation)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("global::Cratis.Arc.IOperationContextAccessor", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("return new object[] { new OrderPlaced(context.Command.Name) };", "STAGE-ESM-019")]
    [InlineData("return 42;", "STAGE-ESM-019")]
    [InlineData("return null;", "STAGE-ESM-019")]
    [InlineData("return (dynamic)new OrderPlaced(context.Command.Name);", "STAGE-ESM-019")]
    [InlineData("", "STAGE-ESM-019")]
    [InlineData("} public class Escaped {", "STAGE-INLINE-INTEGRITY")]
    [InlineData("#endif\nreturn new OrderPlaced(context.Command.Name);", "STAGE-INLINE-INTEGRITY")]
    [InlineData("context.Command.Name = \"changed\"; return new OrderPlaced(context.Command.Name);", "STAGE-ESM-019")]
    [InlineData("return new OrderPlaced(context.Identity.Id);", "STAGE-ESM-015")]
    [InlineData("var alias = context; return new OrderPlaced(alias.Tenant.ToString());", "STAGE-ESM-015")]
    [InlineData("return new OrderPlaced(context?.Identity?.ToString() ?? \"missing\");", "STAGE-ESM-015")]
    [InlineData("var alias = context; return new OrderPlaced(alias?.Tenant?.ToString() ?? \"missing\");", "STAGE-ESM-015")]
    [InlineData("return new OrderPlaced(context[\"Identity\"].ToString());", "STAGE-ESM-015")]
    [InlineData("var alias = context; return new OrderPlaced(alias?[\"Occurred\"]?.ToString() ?? \"missing\");", "STAGE-ESM-015")]
    public void should_refuse_invalid_or_unavailable_bodies_without_partial_artifacts(string body, string code)
    {
        var plan = handler_model.Plan(handler_model.Request(body));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Empty(plan.Artifacts);
    }

    [Theory]
    [InlineData("return new OrderPlaced(context?.Command.Name ?? \"missing\");")]
    [InlineData("var alias = context; return new OrderPlaced(alias?.Command?.Name ?? \"missing\");")]
    [InlineData("return new OrderPlaced(context.Command.Name[0].ToString());")]
    public void should_allow_conditional_snapshot_and_input_element_access(string body)
    {
        var plan = handler_model.Plan(handler_model.Request(body));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }

    [Theory]
    [InlineData("return new OrderPlaced(context.Causation.ToString());")]
    [InlineData("var alias = context; return new OrderPlaced(alias.Occurred.ToString());")]
    public void should_keep_ambient_metadata_unavailable(string body)
    {
        Assert.Contains(handler_model.Plan(handler_model.Request(body)).Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-015");
    }
}
