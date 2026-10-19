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

// Decision 0098 (#867): `decimals <= N` is checked at the live edge exactly as the reference evaluator checks it.
public class with_decimal_places : Specification
{
    [Fact]
    public async Task should_admit_v29_and_match_reference_rule_checks()
    {
        var plan = compiled_plan.From("""
            concept Money : Decimal
              validate
                decimals <= 2 message "Money has at most two decimals"
            module Prices
              feature Pricing
                slice StateChange SetPrice
                  command SetPrice
                    id String identifier
                    price Money
                    produces PriceSet
                      for id
                      price = price
                  event PriceSet
                    price Money
            """);
        plan.Model.LanguageVersion.ShouldEqual(LanguageVersion.V29);
        plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V29);
        new SemanticRuntimeAdmission(plan).Blocking.ShouldBeEmpty();
        var command = plan.Commands.Values.Single();
        var rule = plan.Model.Application.Concepts.Single().Validations.Single();
        var appender = Substitute.For<IAppendSemanticFacts>();
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        foreach (var input in new[] { "0", "19", "19.9", "19.99", "19.990", "19.999", "0.001", "-5.125" })
        {
            var number = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
            var expected = SemanticValidationRules.Check(rule, SemanticValue.Number(number));
            var result = await runtime.Execute(
                command,
                new Dictionary<string, JsonElement>
                {
                    ["id"] = JsonSerializer.SerializeToElement("mug"),
                    ["price"] = JsonSerializer.SerializeToElement(number)
                },
                new ClaimsPrincipal(),
                new(DateTimeOffset.UnixEpoch, "subject", "name", "user"),
                true);
            result.Kind.ShouldEqual(expected.Status == SemanticRuleStatus.Satisfied ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected);
            if (result is SemanticRejected rejected)
            {
                rejected.ValidationFailures.Single().Message.ShouldEqual("Money has at most two decimals");
            }
        }
    }
}
