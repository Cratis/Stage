// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_running_a_model_with_application_triggers : Specification
{
    const string Source = """
        trigger BatchRequested
          batchId String
        module Work
          feature Processing
            slice StateChange Register
              command RegisterBatch
                batchId String identifier
                produces BatchRegistered
                  for batchId
                  batchId = batchId
              event BatchRegistered
                batchId String
            slice Automation Batches
              command FinishBatch
                batchId String identifier
                produces BatchFinished
                  for batchId
                  batchId = batchId
              event BatchFinished
                batchId String
              reaction ProcessBatch
                when BatchRequested
                  batchId
                  invokes FinishBatch
                    batchId = batchId
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;
    SemanticExecutionResult _command = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Work"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "source.play", Source);
        var compiled = new SemanticModelCompiler().Compile("Work", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        var compilation = SemanticExecutionPlan.Compile(compiled.Value.Model);
        Assert.True(compilation.Success, string.Join(';', compilation.Issues));
        _plan = compilation.Plan!;
    }

    async Task Because()
    {
        _admission = new(_plan);
        var runtime = SemanticRuntimeHosting.Create(_plan, Substitute.For<IAppendSemanticFacts>());
        using var lifetime = (IDisposable)runtime;
        var properties = new Dictionary<string, JsonElement> { ["batchId"] = JsonSerializer.SerializeToElement("first") };
        _command = await runtime.Execute(RegisterBatch, properties, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "subject", "name", "user"), true);
    }

    SemanticCommand RegisterBatch => _plan.Commands.Values.Single(command => command.Name == "RegisterBatch");

    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_refuse_the_trigger() => _admission.Entries.Single(entry => entry.Kind == "trigger").Details.ShouldContain("STAGE-ESM-024:");
    [Fact] void should_mark_the_trigger_unsupported() => _admission.Entries.Single(entry => entry.Kind == "trigger").Status.ShouldEqual("unsupported");
    [Fact] void should_say_the_trigger_is_not_executed() => _admission.Entries.Single(entry => entry.Kind == "trigger").Details.ShouldContain("Not executed at runtime");
    [Fact] void should_refuse_the_reaction() => _admission.Entries.Where(entry => entry.Kind == "reaction").ShouldContain(entry => entry.Status == "unsupported" && entry.Details!.Contains("STAGE-ESM-024:", StringComparison.Ordinal));
    [Fact] void should_admit_the_plain_command() => _admission.Entries.Single(entry => entry.Kind == "command" && entry.Artifact == RegisterBatch.Id.ToString()).Status.ShouldEqual("supported");
    [Fact] void should_run_the_plain_command() => _command.ShouldBeOfExactType<SemanticAccepted>();
}
