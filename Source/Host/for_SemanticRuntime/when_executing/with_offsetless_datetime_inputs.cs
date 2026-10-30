// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_offsetless_datetime_inputs : Specification
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(0, true)]
    public async Task should_report_input_rejection_or_explicitly_unsupported_validation(int scenario, bool authorize)
    {
        const string valid = "2026-01-01T00:00:00.0000000Z";
        const string missing = "2026-01-02T00:00:00.0000000";
        var clause = scenario switch
        {
            2 => "to >= \"2026-01-01T00:00:00Z\"",
            3 => "require id == \"window\" or to >= from",
            4 => "id == \"failure\" message \"Rule ran\"",
            _ => "require to >= from"
        };
        var source = $$"""
            policy Member
              require authenticated
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    from DateTime
                    to DateTime
            {{(authorize ? "        authorize Member" : "")}}
                    validate
                      {{clause}}
            {{(scenario == 5 ? "        validate\n          ```csharp\n          yield return \"Rule ran\";\n          ```" : "")}}
            {{(scenario == 4 ? "        produces when id == \"other\" and to >= from\n          Kept\n            for id" : "        produces Kept\n          for id")}}
                  event Kept
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DateInputs"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("DateInputs", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var command = plan.Commands.Values.Single();
        var inputs = new Dictionary<string, string>(StringComparer.Ordinal) { ["id"] = "window", ["from"] = scenario == 1 ? missing : valid, ["to"] = scenario == 1 ? valid : missing };
        var request = SemanticExecutionRequest.Create(command.Id, [.. command.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(inputs[property.Name])))], []);
        var reference = Assert.IsType<SemanticRejected>(new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request));
        var appender = Substitute.For<IAppendSemanticFacts>();
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        var values = inputs.ToDictionary(pair => pair.Key, pair => JsonSerializer.SerializeToElement(pair.Value));
        var execution = await runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), false);
        if (scenario == 5)
        {
            // The reference returns Validation for the missing offset before checking opaque validation.
            // Host refuses implementation bodies before authorization and input validation; Unsupported is not a pass.
            reference.Category.ShouldEqual(SemanticRejectionCategory.Validation);
            reference.Details.ShouldEqual("DateTime command property 'to' requires an explicit offset or Z.");
            var unsupported = Assert.IsType<SemanticUnsupported>(execution);
            unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Command);
            unsupported.Details.ShouldEqual($"STAGE-IMPL-003: Command 'Keep' validation '{command.CodeValidations.Single().RequirementId}' is an implementation body; CommandValidation is not executed by `cratis run` yet (Stage#891 slice S2).");
            unsupported.World.Facts.ShouldBeEmpty();
            await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
            return;
        }

        var result = Assert.IsType<SemanticRejected>(execution);
        result.Category.ShouldEqual(authorize ? SemanticRejectionCategory.Unauthorized : SemanticRejectionCategory.Validation);
        result.Category.ShouldEqual(reference.Category);
        result.Details.ShouldEqual(reference.Details);
        result.World.Facts.ShouldBeEmpty();
        if (!authorize)
        {
            result.ValidationFailures.Length.ShouldEqual(1);
            var mapped = SemanticCommandOutcome.Map(result, CorrelationId.New(), null, command.Name);
            mapped.ValidationResults.Single().Message.ShouldEqual(reference.Details);
        }
        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }
}
