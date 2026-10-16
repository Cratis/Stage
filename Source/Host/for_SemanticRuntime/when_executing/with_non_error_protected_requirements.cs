// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_non_error_protected_requirements : Specification
{
    [Theory]
    [InlineData(SemanticValidationSeverity.Warning, false)]
    [InlineData(SemanticValidationSeverity.Information, false)]
    [InlineData(SemanticValidationSeverity.Warning, true)]
    [InlineData(SemanticValidationSeverity.Information, true)]
    public async Task should_refuse_the_invoked_command_before_rejection_or_append(SemanticValidationSeverity severity, bool validateOnly)
    {
        var plan = Plan(severity);
        var invoked = plan.Commands.Values.Single(command => command.Name == "Consume");
        var blocker = Assert.Single(new SemanticRuntimeAdmission(plan).Blocking);
        blocker.Artifact.ShouldEqual(invoked.Id.ToString());
        blocker.Capability.ShouldEqual("Command");
        Assert.StartsWith("STAGE-ESM-045:", blocker.Details);

        var appender = Substitute.For<IAppendSemanticFacts>();
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        var command = plan.Commands.Values.Single(command => command.Name == "RequestWork");
        var payload = new Dictionary<string, JsonElement>
        {
            ["id"] = JsonSerializer.SerializeToElement("request"),
            ["targetId"] = JsonSerializer.SerializeToElement("target"),
            ["sourceId"] = JsonSerializer.SerializeToElement("missing")
        };
        var result = Assert.IsType<SemanticUnsupported>(await runtime.Execute(command, payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), validateOnly));
        result.Capability.ShouldEqual(SemanticExecutionCapability.Command);
        Assert.StartsWith("STAGE-ESM-045:", result.Details);
        result.Details.ShouldContain("Consume");
        result.World.Facts.ShouldBeEmpty();
        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }

    static SemanticExecutionPlan Plan(SemanticValidationSeverity severity)
    {
        var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => slice.Name == "Use");
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? slice with
            {
                Commands = [.. slice.Commands.Select(command => command.Name == "Consume" ? command with
                {
                    Requirements = [command.Requirements.Single() with { Severity = severity }]
                } : command)]
            } : candidate)] }] }]
        });

        return SemanticExecutionPlan.Compile(model).Plan!;
    }
}
