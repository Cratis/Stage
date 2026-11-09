// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_running_a_model_with_a_command_clock : Specification
{
    const string Source = """
        module Work
          feature Scheduling
            slice StateChange Schedule
              command ScheduleTask
                taskId String identifier
                dueDate Date
                validate
                  dueDate > today
                produces TaskScheduled
                  for taskId
                  taskId = taskId
              command RegisterTask
                taskId String identifier
                produces TaskRegistered
                  for taskId
                  taskId = taskId
              event TaskScheduled
                taskId String
              event TaskRegistered
                taskId String
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;
    SemanticExecutionResult _clockCommand = null!;
    SemanticExecutionResult _plainCommand = null!;

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
        var properties = new Dictionary<string, JsonElement>
        {
            ["taskId"] = JsonSerializer.SerializeToElement("first"),
            ["dueDate"] = JsonSerializer.SerializeToElement("2999-01-01")
        };
        var context = new SemanticCommandOccurrence(DateTimeOffset.UnixEpoch, "subject", "name", "user");
        _clockCommand = await runtime.Execute(Command("ScheduleTask"), properties, new ClaimsPrincipal(), context, true);
        properties.Remove("dueDate");
        _plainCommand = await runtime.Execute(Command("RegisterTask"), properties, new ClaimsPrincipal(), context, true);
    }

    SemanticCommand Command(string name) => _plan.Commands.Values.Single(command => command.Name == name);

    [Fact] void should_be_an_audited_pair() => (_plan.Model.SemanticVersion.Major >= 37).ShouldBeTrue();
    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_refuse_the_clock_command_in_admission() => _admission.Entries.Single(entry => entry.Artifact == Command("ScheduleTask").Id.ToString()).Details.ShouldContain("STAGE-ESM-016:");
    [Fact] void should_mark_the_clock_command_unsupported() => _admission.Entries.Single(entry => entry.Artifact == Command("ScheduleTask").Id.ToString()).Status.ShouldEqual("unsupported");
    [Fact] void should_admit_the_plain_command() => _admission.Entries.Single(entry => entry.Artifact == Command("RegisterTask").Id.ToString()).Status.ShouldEqual("supported");
    [Fact] void should_refuse_the_clock_command_when_executed() => ((SemanticUnsupported)_clockCommand).Details.ShouldContain("STAGE-ESM-016:");
    [Fact] void should_run_the_plain_command() => _plainCommand.ShouldBeOfExactType<SemanticAccepted>();
}
