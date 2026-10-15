// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_generated_defects_after_command_checks : Specification
{
    [Theory]
    [InlineData(false, "{\"name\":\"valid\"}", SemanticRejectionCategory.Unauthorized)]
    [InlineData(false, "{\"name\":123}", SemanticRejectionCategory.Unauthorized)]
    [InlineData(true, "{\"name\":\"\"}", SemanticRejectionCategory.Validation)]
    [InlineData(true, "{\"name\":123}", SemanticRejectionCategory.Contract)]
    [InlineData(true, "{\"name\":\"valid\",\"unknown\":true}", SemanticRejectionCategory.Contract)]
    [InlineData(true, "{\"name\":\"valid\"}", null)]
    public async Task should_preserve_authorization_and_ordinary_input_failures_before_reporting_a_defect(bool authenticated, string json, SemanticRejectionCategory? expected)
    {
        var plan = compiled_plan.From("""
            concept Code : String
              generate ulid case lower
              validate
                matches "^(?!01)[0-7][0-9a-hjkmnp-tv-z]{25}$"
            policy SignedIn
              require authenticated
            module M
              feature F
                slice StateChange S
                  command C
                    code Code generated identifier
                    name String
                    authorize SignedIn
                    validate
                      name not empty message "Name is required"
                    produces event Created
                      name String = name
                    returns code
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "spec" : null));
        var occurrence = new SemanticCommandOccurrence(new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero), "subject", "name", "user");

        foreach (var validateOnly in new[] { true, false })
        {
            SemanticExecutionResult outcome = null!;
            var error = await Catch.Exception(async () => outcome = await runtime.Execute(plan.Commands.Values.Single(), payload, principal, occurrence, validateOnly));
            if (expected is { } category)
            {
                error.ShouldBeNull();
                ((SemanticRejected)outcome).Category.ShouldEqual(category);
                var result = SemanticCommandOutcome.Map(outcome, CorrelationId.New(), null, "C");
                result.HasExceptions.ShouldBeFalse();
                result.IsAuthorized.ShouldEqual(category != SemanticRejectionCategory.Unauthorized);
                result.ValidationResults.Count().ShouldEqual(category == SemanticRejectionCategory.Unauthorized ? 0 : 1);
            }
            else
            {
                error.ShouldBeOfExactType<SemanticCommandExecutionFailed>();
                error.Message.ShouldEqual("Generated fixture 'code' fails concept 'Code' rules");
            }
        }

        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }
}
