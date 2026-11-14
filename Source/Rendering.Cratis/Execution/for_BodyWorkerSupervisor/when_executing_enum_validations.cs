// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler.given;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

public class when_executing_enum_validations : Specification
{
    [Theory]
    [InlineData("open", true)]
    [InlineData("Open", true)]
    [InlineData("closed", false)]
    [InlineData("Closed", false)]
    async Task should_materialize_enum_members_in_command_validation(string member, bool valid)
    {
        var source = an_enum_validation_model.Command("if (context.Artifact.Status != Status.Open) yield return \"Closed\";", iterator: true);
        var result = await Evaluate(source, member, SemanticImplementationRole.CommandValidation);

        Assert.Equal(valid ? "satisfied" : "unsatisfied", result.Outcome);
        string[] expected = valid ? [] : ["Closed"];
        Assert.Equal(expected, result.Messages.ToArray());
        Assert.Null(result.Diagnostic);
    }

    [Theory]
    [InlineData("open", true)]
    [InlineData("Open", true)]
    [InlineData("closed", false)]
    [InlineData("Closed", false)]
    async Task should_materialize_enum_members_in_rule_predicates(string member, bool valid)
    {
        var source = an_enum_validation_model.Command("return context.Value == Status.Open && context.Artifact.Status == context.Value;");
        var result = await Evaluate(source, member, SemanticImplementationRole.RulePredicate);

        Assert.Equal(valid ? "satisfied" : "unsatisfied", result.Outcome);
        Assert.Equal(SemanticValue.Boolean(valid), SemanticValueWire.Read(result.Value!.Value));
        Assert.Empty(result.Messages);
        Assert.Null(result.Diagnostic);
    }

    async Task<BodyEvaluation> Evaluate(string source, string member, SemanticImplementationRole role)
    {
        using var fixture = new a_real_worker();
        var path = Path.Combine(fixture.CacheDirectory, "Model.play");
        await File.WriteAllTextAsync(path, source);
        await using var worker = fixture.Create();
        var unit = Assert.Single((await worker.CompileAsync(path)).Entries);
        Assert.True(unit.Admitted, unit.DiagnosticMessage);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        var command = loaded.Plan.Commands.Values.Single();
        var value = SemanticValue.Text(member);
        var artifact = SemanticValue.Composite([.. command.Properties.Select(property => new SemanticPropertyValue(property.Id, property.Name switch
        {
            "status" => value,
            "recordedAt" => SemanticValue.Text(DateTimeOffset.UnixEpoch.ToString("O")),
            _ => SemanticValue.Text(Guid.Empty.ToString())
        }))]);
        var request = new BodyEvaluationRequest(unit.RequirementId, new Dictionary<string, SemanticValue>
        {
            ["Artifact"] = artifact,
            ["Value"] = role == SemanticImplementationRole.CommandValidation ? artifact : value,
            ["Property"] = SemanticValue.Text(role == SemanticImplementationRole.CommandValidation ? string.Empty : "status")
        }.ToImmutableDictionary(pair => pair.Key, pair => SemanticValueWire.Write(pair.Value), StringComparer.Ordinal));
        return role == SemanticImplementationRole.CommandValidation
            ? await worker.ValidateAsync(request)
            : await worker.EvaluateAsync(request);
    }
}
#endif
