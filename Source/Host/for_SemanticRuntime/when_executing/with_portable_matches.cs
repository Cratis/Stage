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

public class with_portable_matches : Specification
{
    [Theory]
    [InlineData("^.$", "\r", false)]
    [InlineData("^.$", "\u2028", false)]
    [InlineData("^.$", "\u2029", false)]
    [InlineData(@"^\s$", "\u00a0", true)]
    [InlineData(@"^\s$", "\ufeff", true)]
    [InlineData(@"^\s$", "\u0085", false)]
    [InlineData(@"^\S$", "\u00a0", false)]
    [InlineData(@"^[\s]$", "\u2003", true)]
    [InlineData(@"^[^\s]$", "\u3000", false)]
    [InlineData(@"^\w$", "\u0130", false)]
    [InlineData(@"^\W$", "\u0130", true)]
    [InlineData(@"^[\w]$", "\u0130", false)]
    [InlineData(@"^\w$", "é", false)]
    [InlineData(@"^\d$", "\u0661", false)]
    [InlineData("ABC$", "ABC\n", false)]
    [InlineData("B", "ABC", true)]
    [InlineData("^abc$", "ABC", false)]
    [InlineData("^.*$", null, true)]
    public async Task should_preserve_the_reference_match_result(string pattern, string? input, bool expected)
    {
        var literalPattern = pattern.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        var plan = compiled_plan.From($$"""
            module Matches
              feature Checking
                slice StateChange Check
                  command Check
                    value String?
                    validate
                      value matches "{{literalPattern}}"
                    returns value
            """);
        var command = plan.Commands.Values.Single();
        var rule = command.Validations.Single();
        (SemanticValidationRules.Check(rule, input is null ? SemanticValue.Null : SemanticValue.Text(input)).Status == SemanticRuleStatus.Satisfied).ShouldEqual(expected);
        var runtime = SemanticRuntimeHosting.Create(plan, Substitute.For<IAppendSemanticFacts>());
        using var lifetime = (IDisposable)runtime;
        var result = await runtime.Execute(
            command,
            new Dictionary<string, JsonElement> { ["value"] = JsonSerializer.SerializeToElement(input) },
            new ClaimsPrincipal(),
            new(DateTimeOffset.UnixEpoch, "subject", "name", "user"),
            true);
        result.Kind.ShouldEqual(expected ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected);
    }
}
