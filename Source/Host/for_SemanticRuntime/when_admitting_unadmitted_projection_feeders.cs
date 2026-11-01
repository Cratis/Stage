// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_admitting_unadmitted_projection_feeders : a_locally_unadmitted_projection
{
    SemanticRuntimeAdmission _admission = null!;
    SemanticExecutionResult _result = null!;
    SemanticExecutionResult _transitiveResult = null!;
    IAppendSemanticFacts _append = null!;

    async Task Because()
    {
        _admission = new(_plan);
        _append = Substitute.For<IAppendSemanticFacts>();
        using var runtime = new SemanticRuntime(_plan, _append);
        var properties = new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        };
        var occurrence = new SemanticCommandOccurrence(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner");
        _result = await runtime.Execute(Command("RegisterProject"), properties, new ClaimsPrincipal(), occurrence, false);
        var startProperties = new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("4fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["parentId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6")
        };
        _transitiveResult = await runtime.Execute(Command("StartProject"), startProperties, new ClaimsPrincipal(), occurrence, false);
    }

    [Fact] void should_refuse_the_direct_command() => AssertRefused(_command.Id.ToString(), "command");
    [Fact] void should_refuse_the_reaction() => AssertRefused(_plan.Reactions.Single().Id.ToString(), "reaction");
    [Fact] void should_refuse_the_transitive_command() => AssertRefused(Command("StartProject").Id.ToString(), "command");
    [Fact] void should_support_the_unrelated_command() => Assert.Contains(_admission.Entries, entry => entry.Artifact == Command("TouchProject").Id.ToString() && entry.Status == "supported");
    [Fact] void should_not_block_the_model() => Assert.Empty(_admission.Blocking);
    [Fact] void should_name_the_projection_in_the_execution_refusal() => Assert.Contains("RefusedProjection", Assert.IsType<SemanticUnsupported>(_result).Details);
    [Fact] void should_name_the_projection_in_the_transitive_execution_refusal() => Assert.Contains("RefusedProjection", Assert.IsType<SemanticUnsupported>(_transitiveResult).Details);
    [Fact] Task should_not_append() => _append.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());

    void AssertRefused(string id, string kind)
    {
        var entry = Assert.Single(_admission.Entries, entry => entry.Artifact == id && entry.Kind == kind);
        Assert.Equal("unsupported", entry.Status);
        Assert.Contains("RefusedProjection", entry.Details);
    }
}
