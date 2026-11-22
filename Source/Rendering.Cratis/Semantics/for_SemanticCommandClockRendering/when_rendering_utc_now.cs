// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
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
    static readonly Dictionary<ProductionCase, (string Condition, string At, bool Taken)> _productionCases = new()
    {
        [ProductionCase.EquivalentOffset] = ("at == now", "2026-01-01T19:00:00.0000000-05:00", true),
        [ProductionCase.BeforeLowerBound] = ("at >= now", "2026-01-01T23:59:59.9999999Z", false),
        [ProductionCase.OneTickBefore] = ("at < now", "2026-01-01T23:59:59.9999999Z", true),
        [ProductionCase.ClockOnLeft] = ("now < at", "2026-01-02T00:00:00.0000001Z", true),
        [ProductionCase.Conjunction] = ("at >= now and at < now + 1 minute", "2026-01-02T00:00:59.9999999Z", true),
        [ProductionCase.Disjunction] = ("at < now or at > now + 1 hour", "2026-01-02T01:00:00.0000000Z", false),
        [ProductionCase.NegativeDay] = ("at <= now - 1 day", "2026-01-01T00:00:00.0000000Z", true),
        [ProductionCase.PositiveMinute] = ("at > now + 1 minute", "2026-01-02T00:01:00.0000001Z", true)
    };

    public enum ZoneCase
    {
        ConfigurableUtc,
        NonUtc
    }

    public enum ProductionCase
    {
        EquivalentOffset,
        BeforeLowerBound,
        OneTickBefore,
        ClockOnLeft,
        Conjunction,
        Disjunction,
        NegativeDay,
        PositiveMinute
    }

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
    public void should_admit_the_audited_utc_now_condition()
    {
        var context = Context("at >= now");
        var plan = invoice_model.Plan(context.Request.Model);
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
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
        code.ShouldContain("var occurred = stageClock;");
        code.ShouldNotContain("FromUnixTimeMilliseconds");
        code.ShouldContain("CommandReceiptTime.ValidatedInstant(validation.RootContextData)");
        code.ShouldNotContain("(DateTimeOffset)validation.RootContextData");
        code.ShouldEqual(SemanticStateChangeArtifactRenderer.Render(slice, context).Content);
        code.ShouldNotContain("TimeProvider");
        code.ShouldNotContain("UtcNow");
    }

    [Theory]
    [InlineData(ZoneCase.ConfigurableUtc)]
    [InlineData(ZoneCase.NonUtc)]
    public void should_refuse_configurable_or_non_utc_zones(ZoneCase scenario)
    {
        var zone = scenario == ZoneCase.NonUtc ? "America/New_York" : "UTC";
        var configurable = scenario == ZoneCase.ConfigurableUtc;
        var context = Context("at >= now");
        var application = context.Application with { TimeZone = context.Application.TimeZone! with { Id = zone, Configurable = configurable } };
        SemanticCommandClockSupport.Supports(application, context.Commands.Values.Single()).ShouldBeFalse();
    }

    [Theory]
    [InlineData(ProductionCase.EquivalentOffset)]
    [InlineData(ProductionCase.BeforeLowerBound)]
    [InlineData(ProductionCase.OneTickBefore)]
    [InlineData(ProductionCase.ClockOnLeft)]
    [InlineData(ProductionCase.Conjunction)]
    [InlineData(ProductionCase.Disjunction)]
    [InlineData(ProductionCase.NegativeDay)]
    [InlineData(ProductionCase.PositiveMinute)]
    public void should_select_conditional_productions_at_the_authored_instant(ProductionCase scenario)
    {
        var (condition, at, taken) = _productionCases[scenario];
        var context = Context(condition);
        var command = Conditional(context);
        var specification = Fixture(command, at);
        SemanticProductionSelection.Taken(context.Application, command, specification).Length.ShouldEqual(taken ? 1 : 0);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void should_not_round_tick_precision_comparisons(int difference, bool equal)
    {
        var context = Context("at == now");
        var command = context.Commands.Values.Single();
        var clock = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero).AddTicks(1);
        var values = Fixture(command, clock.AddTicks(difference).ToString("O", CultureInfo.InvariantCulture)).When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        SemanticProductionComparison.Compare((SemanticComparison)command.Requirements[0].Condition, values, (static () => new InvalidOperationException(), static () => new InvalidOperationException()), (null, clock)).ShouldEqual(equal);
    }

    [Fact]
    public void should_not_select_a_clock_branch_using_an_implicit_clock()
    {
        var context = Context("at >= now");
        var command = Conditional(context);
        var specification = Fixture(command, "2026-01-02T00:00:00.0000000Z") with { GivenClock = null };
        SemanticProductionSelection.Taken(context.Application, command, specification).ShouldEqual(command.Produces);
        SemanticOpaqueBodyDependencies.Rejection(context, specification).ShouldContain("given clock");
    }

    [Fact]
    public void should_fail_closed_for_a_missing_or_wrong_validation_receipt()
    {
        var code = CommandReceiptTimeRendering.Render("InvoiceApp").Content;
        code.ShouldContain("TryGetValue(\"Cratis.Stage.CommandReceipt\"");
        code.ShouldContain("instant != default");
        code.ShouldContain("throw new CommandReceiptTimeUnavailable()");
        code.ShouldNotContain("UtcNow");
    }

    [Fact]
    public void should_preserve_ticks_for_an_unconditional_clock_commands_occurrence()
    {
        var context = Context("at >= now");
        var code = SemanticStateChangeArtifactRenderer.Render(context.SelectedSlices().Single(), context).Content;
        code.ShouldContain("CommandReceiptTime.InstantAtReceipt(operation)");
        code.ShouldNotContain("CommandReceiptTime.OccurredAtReceipt");
        code.ShouldNotContain("FromUnixTimeMilliseconds");
    }

    static SemanticCommand Conditional(SemanticApplicationContext context)
    {
        var command = context.Commands.Values.Single();
        return command with { Produces = [command.Produces[0] with { When = command.Requirements[0].Condition }] };
    }

    static SemanticSpecification Fixture(SemanticCommand command, string at)
    {
        var values = command.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(property.Name == "id" ? "window" : at))).ToImmutableArray();
        return new(default, "Clock", [], [], new(command.Id, values), [], [], [], []) { GivenClock = "2026-01-02T00:00:00.0000000Z" };
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
