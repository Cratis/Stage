// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_command_clock_validations : Specification
{
    public enum RuleCase
    {
        Primitive,
        Concept,
        EquivalentOffset,
        DefaultMessage,
        AuthoredMessage,
        OptionalPrimitive,
        OptionalConcept,
        MissingClock
    }

    [Theory]
    [InlineData(RuleCase.Primitive)]
    [InlineData(RuleCase.Concept)]
    [InlineData(RuleCase.EquivalentOffset)]
    [InlineData(RuleCase.DefaultMessage)]
    [InlineData(RuleCase.AuthoredMessage)]
    [InlineData(RuleCase.OptionalPrimitive)]
    [InlineData(RuleCase.OptionalConcept)]
    [InlineData(RuleCase.MissingClock)]
    public async Task should_agree_with_the_reference_without_reading_an_options_clock(RuleCase scenario)
    {
        var plan = Compile(scenario);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        var report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new() { Clock = new UnexpectedClock() });
        var result = report.Results.Single();
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        result.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
    }

    [Theory]
    [InlineData(RuleCase.OptionalPrimitive)]
    [InlineData(RuleCase.OptionalConcept)]
    public void should_allow_optional_absence_in_constructed_semantic_inputs(RuleCase scenario)
    {
        var plan = Compile(scenario);
        var command = plan.Commands.Values.Single();
        var at = command.Properties.Single(property => property.Name == "at").Id;
        var specification = plan.Specifications.Values.Single();
        var invocation = specification.When! with
        {
            Values = [.. specification.When!.Values.Select(value => value.TargetProperty == at ? value with { Value = SemanticValue.Null } : value)]
        };
        var absent = specification with { When = invocation };
        SemanticRuleEvaluation.Failures(command, absent, plan.Model.Application).ShouldBeEmpty();
        var instant = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var request = SemanticExecutionRequest.Create(command.Id, invocation.Values, []) with { Occurrence = new(instant, "caller", "Caller", "caller") };
        new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request).ShouldBeOfExactType<SemanticAccepted>();
    }

    static SemanticExecutionPlan Compile(RuleCase scenario)
    {
        var type = scenario switch
        {
            RuleCase.Concept => "Moment",
            RuleCase.OptionalPrimitive => "DateTime optional",
            RuleCase.OptionalConcept => "Moment optional",
            _ => "DateTime"
        };
        var input = scenario switch
        {
            RuleCase.EquivalentOffset => "2026-01-01T19:00:00.0000000-05:00",
            RuleCase.DefaultMessage or RuleCase.AuthoredMessage => "2026-01-01T23:59:59.9999999Z",
            _ => "2026-01-02T00:00:00.0000000Z"
        };
        var clock = scenario == RuleCase.MissingClock ? string.Empty : "        given clock \"2026-01-02T00:00:00.0000000Z\"\n";
        var value = $"          at = \"{input}\"\n";
        var message = scenario == RuleCase.AuthoredMessage ? " message \"Choose a future instant.\"" : string.Empty;
        var expected = scenario switch
        {
            RuleCase.DefaultMessage => "        then error \"A value must be at least '2026-01-02T00:00:00.0000000Z'.\"",
            RuleCase.AuthoredMessage => "        then error \"Choose a future instant.\"",
            RuleCase.MissingClock => "        then error \"A command using a clock operand needs the instant of the command; the execution request supplies no occurrence.\"",
            _ => "        then Kept\n          recordedAt = \"2026-01-02T00:00:00.0000000Z\""
        };
        var source = $$"""
            concept Moment : DateTime
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at {{type}}
                    validate
                      at >= now{{message}}
                    produces Kept
                      for id
                      recordedAt = $context.occurred
                  event Kept
                    recordedAt DateTime
                  specification Rule
            """ + "\n" + clock + "        when Keep\n          id = \"window\"\n" + value + expected;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ClockValidationExecution"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("ClockValidationExecution", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model);
        Assert.True(plan.Success, string.Join(';', plan.Issues));
        return plan.Plan!;
    }

    sealed class UnexpectedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("The options clock must not supply a command occurrence.");
    }
}
