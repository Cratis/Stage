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

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_generated_defects_after_command_checks : Specification
{
    [Theory]
    [InlineData(false, "valid", SemanticRejectionCategory.Unauthorized)]
    [InlineData(true, "", SemanticRejectionCategory.Validation)]
    [InlineData(true, "valid", null)]
    public async Task should_check_the_invoked_commands_policy_and_input_before_reporting_its_defect(bool systemIdentity, string name, SemanticRejectionCategory? expected)
    {
        var plan = compiled_plan.From($$"""
            concept Code : String
              generate ulid case lower
              validate
                matches "^(?!01)[0-7][0-9a-hjkmnp-tv-z]{25}$"
            policy SignedIn
              require authenticated
            module M
              feature F
                slice StateChange Start
                  command Start
                    id String identifier
                    name String
                    produces event Started
                      name String = name
                slice Automation FollowUp
                  command Complete
                    code Code generated
                    name String
                    authorize SignedIn
                    validate
                      name not empty message "Name is required"
                    returns code
                  reaction Worker
            {{(systemIdentity ? "        runs as system" : string.Empty)}}
                    when Started
                      name
                      invokes Complete
                        name = name
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        SemanticExecutionResult outcome = null!;
        var payload = new Dictionary<string, JsonElement> { ["id"] = JsonSerializer.SerializeToElement("one"), ["name"] = JsonSerializer.SerializeToElement(name) };
        var occurrence = new SemanticCommandOccurrence(new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero), "subject", "name", "user");
        var error = await Catch.Exception(async () => outcome = await runtime.Execute(
            plan.Commands.Values.Single(command => command.Name == "Start"), payload, new ClaimsPrincipal(), occurrence, false));

        if (expected is { } category)
        {
            error.ShouldBeNull();
            ((SemanticRejected)outcome).Category.ShouldEqual(category);
            outcome.World.Facts.ShouldBeEmpty();
            var result = SemanticCommandOutcome.Map(outcome, CorrelationId.New(), null, "Start");
            result.HasExceptions.ShouldBeFalse();
            result.IsAuthorized.ShouldEqual(category != SemanticRejectionCategory.Unauthorized);
            result.ValidationResults.Count().ShouldEqual(category == SemanticRejectionCategory.Unauthorized ? 0 : 1);
        }
        else
        {
            error.ShouldBeOfExactType<SemanticCommandExecutionFailed>();
            error.Message.ShouldEqual("Generated fixture 'code' fails concept 'Code' rules");
        }

        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }
}
