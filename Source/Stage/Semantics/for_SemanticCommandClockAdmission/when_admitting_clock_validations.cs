// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandClockAdmission;

public class when_admitting_clock_validations : Specification
{
    public enum ClockCase
    {
        Primitive,
        Concept,
        OptionalPrimitive,
        OptionalConcept,
        MinuteAhead,
        HourBefore,
        DayBefore,
        Today,
        DateOfNow,
        Calendar,
        NonUtc,
        Configurable,
        ContextMember
    }

    [Theory]
    [InlineData(ClockCase.Primitive)]
    [InlineData(ClockCase.Concept)]
    [InlineData(ClockCase.OptionalPrimitive)]
    [InlineData(ClockCase.OptionalConcept)]
    [InlineData(ClockCase.MinuteAhead)]
    [InlineData(ClockCase.HourBefore)]
    [InlineData(ClockCase.DayBefore)]
    public void should_admit_only_the_indexed_datetime_command_rule(ClockCase scenario)
    {
        foreach (var kind in new[] { SemanticValidationRuleKind.Equal, SemanticValidationRuleKind.NotEqual, SemanticValidationRuleKind.GreaterThan, SemanticValidationRuleKind.GreaterThanOrEqual, SemanticValidationRuleKind.LessThan, SemanticValidationRuleKind.LessThanOrEqual })
        {
            var plan = Compile(scenario, kind);
            SemanticRunAdmission.RunStanding(plan.Model, new HashSet<SemanticId>()).UnknownMembers.ShouldBeEmpty();
            SemanticRunAdmission.CommandRangeRefusal(plan, plan.Commands.Values.Single()).ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(ClockCase.Today)]
    [InlineData(ClockCase.DateOfNow)]
    [InlineData(ClockCase.Calendar)]
    [InlineData(ClockCase.NonUtc)]
    [InlineData(ClockCase.Configurable)]
    [InlineData(ClockCase.ContextMember)]
    public void should_keep_unsupported_clock_scopes_refused(ClockCase scenario)
    {
        var plan = Compile(scenario);
        SemanticRunAdmission.RunStanding(plan.Model, new HashSet<SemanticId>()).UnknownMembers.ShouldNotBeEmpty();
        SemanticRunAdmission.CommandRangeRefusal(plan, plan.Commands.Values.Single()).ShouldNotBeNull();
    }

    static SemanticExecutionPlan Compile(ClockCase scenario, SemanticValidationRuleKind kind = SemanticValidationRuleKind.GreaterThanOrEqual)
    {
        const string declaration = "concept Moment : DateTime\n";
        var calendar = scenario == ClockCase.Calendar ? "        calendar \"UTC\"\n" : string.Empty;
        var source = Prefix(scenario) + declaration + $$"""
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at {{PropertyType(scenario)}}
            """ + "\n" + calendar + $$"""
                    validate
                      at {{Operation(kind)}} {{Clock(scenario)}}
                    produces Kept
                      for id
                  event Kept
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ClockValidationAdmission"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("ClockValidationAdmission", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model);
        Assert.True(plan.Success, string.Join(';', plan.Issues));
        return plan.Plan!;
    }

    static string PropertyType(ClockCase scenario) => scenario switch
    {
        ClockCase.Concept => "Moment",
        ClockCase.OptionalPrimitive => "DateTime optional",
        ClockCase.OptionalConcept => "Moment optional",
        ClockCase.Today or ClockCase.DateOfNow => "Date",
        _ => "DateTime"
    };

    static string Clock(ClockCase scenario) => scenario switch
    {
        ClockCase.MinuteAhead => "now + 1 minute",
        ClockCase.HourBefore => "now - 1 hour",
        ClockCase.DayBefore => "now - 1 day",
        ClockCase.Today => "today",
        ClockCase.DateOfNow => "date of now",
        _ => "now"
    };

    static string Prefix(ClockCase scenario) => scenario switch
    {
        ClockCase.NonUtc => "time zone \"Europe/Oslo\"\n",
        ClockCase.Configurable => "time zone configurable default \"UTC\"\n",
        ClockCase.ContextMember => "context\n  billingZone time zone\n",
        _ => string.Empty
    };

    static string Operation(SemanticValidationRuleKind kind) => kind switch
    {
        SemanticValidationRuleKind.Equal => "==",
        SemanticValidationRuleKind.NotEqual => "!=",
        SemanticValidationRuleKind.GreaterThan => ">",
        SemanticValidationRuleKind.GreaterThanOrEqual => ">=",
        SemanticValidationRuleKind.LessThan => "<",
        SemanticValidationRuleKind.LessThanOrEqual => "<=",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
