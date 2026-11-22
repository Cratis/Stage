// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandClockRendering;

public class when_rendering_utc_now
{
    [Theory]
    [InlineData("at == now")]
    [InlineData("at != now")]
    [InlineData("now < at")]
    [InlineData("now <= at")]
    [InlineData("at > now + 1 hour")]
    [InlineData("at >= now - 1 day")]
    [InlineData("at >= now and at <= now + 1 minute")]
    [InlineData("at < now or at > now")]
    public void should_render_instant_comparisons_without_a_provider(string condition)
    {
        var context = Context(condition);
        var command = context.Commands.Values.Single();
        Assert.True(SemanticCommandClockSupport.Supports(context.Application, command), System.Text.Json.JsonSerializer.Serialize(new { context.Application.TimeZone, context.Application.ContextMembers, command.Calendar, command.Requirements, command.Properties, context.Application.Concepts }));
        SemanticConditionRendering.CanRender(command.Requirements[0].Condition, command.Properties, context).ShouldBeTrue();
        var builder = new CSharpCodeBuilder();
        SemanticRequirementRendering.Render(builder, command.Requirements[0], command, context, "receipt");
        var code = builder.ToString();
        code.ShouldContain(".CompareTo(");
        code.ShouldContain("receipt");
        code.ShouldNotContain("TimeProvider");
        code.ShouldNotContain("UtcNow");
    }

    [Fact]
    public void should_refuse_optional_and_collection_operands()
    {
        var context = Context("at >= now");
        var command = context.Commands.Values.Single();
        var property = command.Properties.Single(property => property.Name == "at");
        foreach (var type in new[] { property.Type with { IsOptional = true }, property.Type with { IsCollection = true } })
        {
            var properties = command.Properties.Select(candidate => candidate.Id == property.Id ? candidate with { Type = type } : candidate).ToArray();
            SemanticCommandClockSupport.Supports(command.Requirements[0].Condition, properties, context.Application).ShouldBeFalse();
        }
    }

    [Fact]
    public void should_keep_the_standing_refusal_until_activation()
    {
        var context = Context("at >= now");
        var plan = invoice_model.Plan(context.Request.Model);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-016").ShouldBeTrue();
    }

    [Fact]
    public void should_share_the_active_receipt_between_validator_and_handler()
    {
        var context = Context("at >= now");
        var located = context.SelectedSlices().Single();
        var command = located.Slice.Commands.Single();
        var conditional = command with { Produces = [command.Produces[0] with { When = command.Requirements[0].Condition }] };
        var slice = located with { Slice = located.Slice with { Commands = [conditional] } };
        var code = SemanticStateChangeArtifactRenderer.Render(slice, context).Content;
        code.ShouldContain("IOperationContextAccessor operation");
        code.ShouldContain("var stageClock = CommandReceiptTime.InstantAtReceipt(operation)");
        code.ShouldContain("RootContextData[\"Cratis.Stage.CommandReceipt\"] = CommandReceiptTime.InstantAtReceipt(operation)");
        code.Split("CommandReceiptTime.InstantAtReceipt(operation)", StringSplitOptions.None).Length.ShouldEqual(3);
        code.ShouldContain("FromUnixTimeMilliseconds(stageClock.ToUnixTimeMilliseconds())");
        code.ShouldEqual(SemanticStateChangeArtifactRenderer.Render(slice, context).Content);
        code.ShouldNotContain("TimeProvider");
        code.ShouldNotContain("UtcNow");
    }

    [Theory]
    [InlineData("UTC", true)]
    [InlineData("America/New_York", false)]
    public void should_refuse_configurable_or_non_utc_zones(string zone, bool configurable)
    {
        var context = Context("at >= now");
        var application = context.Application with { TimeZone = context.Application.TimeZone! with { Id = zone, Configurable = configurable } };
        SemanticCommandClockSupport.Supports(application, context.Commands.Values.Single()).ShouldBeFalse();
    }

    static SemanticApplicationContext Context(string condition)
    {
        var model = invoice_model.Compile($$"""
            concept Moment : DateTime
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at Moment
                    validate
                      require {{condition}}
                    produces Kept
                      for id
                  event Kept
            """);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, CratisRendering.CreateProfile("InvoiceApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        return new(request, options);
    }
}
