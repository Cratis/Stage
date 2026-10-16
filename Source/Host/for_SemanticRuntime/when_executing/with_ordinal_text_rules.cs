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

public class with_ordinal_text_rules : Specification
{
    [Theory]
    [InlineData("contains")]
    [InlineData("not contains")]
    [InlineData("starts with")]
    [InlineData("ends with")]
    public async Task should_admit_v24_and_match_reference_rule_checks(string spelling)
    {
        foreach (var operand in new[] { "ABC", "" })
        {
            var plan = compiled_plan.From($$"""
                module TextRules
                  feature Checking
                    slice StateChange Choose
                      command Choose
                        reference String?
                        validate
                          reference {{spelling}} "{{operand}}"
                        returns reference
                """);
            plan.Model.LanguageVersion.ShouldEqual(LanguageVersion.V24);
            plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V24);
            new SemanticRuntimeAdmission(plan).Blocking.ShouldBeEmpty();
            var command = plan.Commands.Values.Single();
            var appender = Substitute.For<IAppendSemanticFacts>();
            var runtime = SemanticRuntimeHosting.Create(plan, appender);
            using var lifetime = (IDisposable)runtime;
            foreach (var input in new[] { "ABC", "abc", "xABCx", "ABCx", "xABC", "", null, "AB\0C", "ABC\n" })
            {
                var expected = SemanticValidationRules.Check(command.Validations.Single(), input is null ? SemanticValue.Null : SemanticValue.Text(input));
                var result = await runtime.Execute(
                    command,
                    new Dictionary<string, JsonElement> { ["reference"] = JsonSerializer.SerializeToElement(input) },
                    new ClaimsPrincipal(),
                    new(DateTimeOffset.UnixEpoch, "subject", "name", "user"),
                    true);
                result.Kind.ShouldEqual(expected.Status == SemanticRuleStatus.Satisfied ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected);
                if (result is SemanticRejected rejected)
                {
                    rejected.ValidationFailures.Single().Message.ShouldEqual(expected.Message);
                }
            }
            await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
        }
    }
}
