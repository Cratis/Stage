// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_executing_a_concept_rule_body : Specification
{
    string _directory = null!;
    LoadedSemanticModel _loaded = null!;
    SemanticBodyExecution _bodies = null!;
    IAppendSemanticFacts _appender = null!;
    SemanticExecutionResult _accepted = null!;
    SemanticExecutionResult _rejected = null!;
    Dictionary<string, SemanticUnsupportedCapability?> _refusals = null!;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-concept-worker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_directory, "Validations"));
        var path = Path.Combine(_directory, "Model.play");
        await File.WriteAllTextAsync(path, Model);
        await File.WriteAllTextAsync(Path.Combine(_directory, "Validations", "Reason.cs"), "string reason = context.Value; return reason.Length <= 200;");
        _loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        var notes = new List<SemanticModelLoadDiagnostic>();
        var settings = new SemanticBodyWorkerOptions(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
        {
            CacheDirectory = Path.Combine(_directory, "cache"),
            CallTimeoutMilliseconds = 30000
        };
        _bodies = await SemanticBodyWorkerHosting.Start(_loaded, path, settings, notes);
        Assert.True(_bodies is not null, string.Join(';', notes.Select(note => note.Message)));
        _appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var tail = ulong.MaxValue;
        ((ISemanticFactTail)_appender).Tail().Returns(_ => tail);
        _appender.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(_ =>
        {
            tail = 0;
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var runtime = SemanticRuntimeHosting.Create(_loaded.Plan, _appender, SemanticWorld.Empty, _bodies);
        using var lifetime = (IDisposable)runtime;
        _accepted = await Execute(runtime, "Not a fit");
        _rejected = await Execute(runtime, new string('x', 201));
        _refusals = _loaded.Plan.Specifications.Values.ToDictionary(specification => specification.Name, specification => SemanticRunAdmission.Check(_loaded.Plan, specification, _bodies));
    }

    [Fact] void should_accept_a_value_the_body_accepts() => Assert.IsType<SemanticAccepted>(_accepted);
    [Fact] void should_reject_a_value_the_body_must_reject() => Assert.IsType<SemanticRejected>(_rejected).Details.ShouldEqual("Please keep the tone calm");
    [Fact] async Task should_not_append_the_rejected_value() => await _appender.Received(1).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    [Fact] void should_admit_the_accepted_specification() => Assert.Null(_refusals["Accepted"]);
    [Fact] void should_admit_the_rejected_specification() => Assert.Null(_refusals["Rejected"]);

    Task<SemanticExecutionResult> Execute(ISemanticRuntime runtime, string reason) => runtime.Execute(
        _loaded.Plan.Commands.Values.Single(),
        new Dictionary<string, JsonElement>
        {
            ["talkId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["reason"] = JsonSerializer.SerializeToElement(reason)
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"),
        false);

    void Destroy() => Directory.Delete(_directory, true);

    const string Model = """
        concept TalkId : Uuid
        concept Reason : String
          validate
            rule BeCalm message "Please keep the tone calm"
              file Validations/Reason.cs
        module CallForPapers
          feature Review
            slice StateChange Declining
              command DeclineTalk
                talkId TalkId identifier
                reason Reason
                produces TalkDeclined
                  for talkId
                  reason = reason
              event TalkDeclined
                reason Reason
              specification Accepted
                when DeclineTalk
                  talkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  reason = "Not a fit"
                then TalkDeclined
                  reason = "Not a fit"
              specification Rejected
                when DeclineTalk
                  talkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  reason = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
                then error "Please keep the tone calm"
        """;
}
