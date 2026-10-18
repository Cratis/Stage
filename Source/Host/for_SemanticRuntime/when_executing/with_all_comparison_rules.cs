// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_all_comparison_rules : Specification
{
    [Theory]
    [InlineData(">", "Int", "[1, 2]", "[1, 0]", "error")]
    [InlineData(">=", "Decimal", "[0, 0.5]", "[0.5, -0.1]", "warning")]
    [InlineData(">", "Count", "[1, 2]", "[1, 0]", "information")]
    [InlineData(">=", "Amount", "[0, 0.5]", "[0.5, -0.1]", "error")]
    public async Task should_match_reference_collection_checks(string comparison, string type, string accepted, string rejected, string severity)
    {
        var plan = compiled_plan.From($$"""
            concept Count : Int
            concept Amount : Decimal
            module Comparisons
              feature Checking
                slice StateChange Check
                  command Check
                    values {{type}}[] optional
                    answer Bool
                    validate
                      values all {{comparison}} 0 severity {{severity}} message "All values must meet the bound"
                    returns answer
                  specification Empty
                    when Check
                      values = []
                      answer = true
                    then returns true
            """);
        new SemanticRuntimeAdmission(plan).Entries.ShouldEachConformTo(entry => entry.Status == "supported");
        var command = plan.Commands.Values.Single();
        var rule = command.Validations.Single();
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        foreach (var input in new[] { accepted, rejected, "[]", "null" })
        {
            var json = JsonSerializer.Deserialize<JsonElement>(input);
            var value = json.ValueKind == JsonValueKind.Null ? SemanticValue.Null : SemanticValue.Array([.. json.EnumerateArray().Select(element => SemanticValue.Number(element.GetDecimal()))]);
            var expected = SemanticValidationRules.Check(rule, value);
            var result = await runtime.Execute(command, new Dictionary<string, JsonElement> { ["values"] = json, ["answer"] = JsonSerializer.SerializeToElement(true) }, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "subject", "name", "user"), false);
            result.Kind.ShouldEqual(expected.Status == SemanticRuleStatus.Satisfied ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected);
            if (result is SemanticRejected failure)
            {
                failure.ValidationFailures.Single().Message.ShouldEqual(expected.Message);
                failure.ValidationFailures.Single().Severity.ShouldEqual(rule.Severity);
            }
        }
        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }
}
