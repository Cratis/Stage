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

public class with_reach_scoped_bodies : a_semantic_runtime
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;
    SemanticExecutionResult _clean = null!;
    SemanticExecutionResult _feeding = null!;
    SemanticExecutionResult _query = null!;

    void Establish()
    {
        const string source = """
            module Work
              feature Items
                slice StateChange Manage
                  command Clean
                    id String identifier
                    produces Cleaned
                      for id
                  command Feed
                    id String identifier
                    produces Changed
                      for id
                  event Cleaned
                  event Changed
                slice StateView View
                  readmodel Snapshot
                    id String key
                  query SnapshotById => Snapshot?
                    by id String
                  reducer Fold => Snapshot
                    on Changed
                      ```csharp
                      return context.State;
                      ```
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("HostBodyReach"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("HostBodyReach", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _admission = new(_plan);
    }

    async Task Because()
    {
        var runtime = SemanticRuntimeHosting.Create(_plan, (IAppendSemanticFacts)_appender);
        using var lifetime = (IDisposable)runtime;
        var payload = new Dictionary<string, JsonElement> { ["id"] = JsonSerializer.SerializeToElement("one") };
        var occurrence = new SemanticCommandOccurrence(DateTimeOffset.UtcNow, "subject", "name", "user");
        _clean = await runtime.Execute(_plan.Commands.Values.Single(command => command.Name == "Clean"), payload, new ClaimsPrincipal(), occurrence, false);
        _feeding = await runtime.Execute(_plan.Commands.Values.Single(command => command.Name == "Feed"), payload, new ClaimsPrincipal(), occurrence, false);
        _query = await runtime.Query(_plan.Queries.Values.Single(), SemanticValue.Text("one"), new ClaimsPrincipal());
    }

    [Fact] void should_accept_the_unrelated_command() => _clean.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_only_the_unrelated_event() => ((SemanticAccepted)_clean).Facts.Single().EventContract.ShouldEqual(_plan.Events.Values.Single(@event => @event.Name == "Cleaned").Id);
    [Fact] void should_refuse_the_feeding_command() => ((SemanticUnsupported)_feeding).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_identify_the_reached_reducer() => ((SemanticUnsupported)_feeding).Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold'");
    [Fact] void should_refuse_the_reducer_query() => ((SemanticUnsupported)_query).Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold'");
    [Fact] void should_not_block_the_live_model() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_admit_the_unrelated_command() => Entry("Clean").Status.ShouldEqual("supported");
    [Fact] void should_report_the_feeding_command_refusal() => Entry("Feed").Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold'");
    [Fact] void should_report_the_query_refusal() => _admission.Entries.Single(entry => entry.Kind == "query").Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold'");
    [Fact] async Task should_append_only_once() => await ((IAppendSemanticFacts)_appender).Received(1).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());

    SemanticAdmissionEntry Entry(string name) => _admission.Entries.Single(entry => entry.Kind == "command" && entry.Artifact == _plan.Commands.Values.Single(command => command.Name == name).Id.ToString());
}
