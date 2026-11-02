// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

[PerFactSpecificationLifecycle]
public class with_competing_protected_decisions : Specification
{
    static readonly TimeSpan _limit = TimeSpan.FromSeconds(10);

    SemanticExecutionPlan _plan = null!;
    ISemanticRuntime _runtime = null!;
    a_semantic_fact_log _log = null!;

    void Establish()
    {
        _plan = compiled_plan.From(a_protected_invoice_model.Source);
        _log = new();
        _runtime = _log.Runtime(_plan, SemanticWorld.Empty);
    }

    void Destroy() => ((IDisposable)_runtime).Dispose();

    [Fact]
    public async Task should_let_the_second_command_see_the_first_commands_fact()
    {
        await Register("inv-1");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _log.PauseBeforeAppend = () =>
        {
            _log.PauseBeforeAppend = null;
            entered.SetResult();
            return release.Task;
        };

        var first = Execute("IssueInvoice", "inv-1");
        await entered.Task.WaitAsync(_limit);
        var second = Execute("IssueInvoice", "inv-1");
        await Task.Delay(200);
        second.IsCompleted.ShouldBeFalse();
        _log.Attempts.Count.ShouldEqual(2);

        release.SetResult();
        var accepted = Assert.IsType<SemanticAccepted>(await first.WaitAsync(_limit));
        var rejected = Assert.IsType<SemanticRejected>(await second.WaitAsync(_limit));
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
        rejected.Details.ShouldEqual("Only a draft invoice can be issued");
        _log.Writes.ShouldEqual(2);
        IssuedCount(accepted).ShouldEqual(1);
    }

    [Fact]
    public async Task should_reject_a_decision_whose_read_changed_before_its_append()
    {
        // A second host replica wins the race: its fact lands in the shared log between this runtime's read and its append.
        var replicaLog = new a_semantic_fact_log();
        using var replica = (IDisposable)replicaLog.Runtime(_plan, SemanticWorld.Empty);
        var replicaRuntime = (ISemanticRuntime)replica;
        await Run(replicaRuntime, "RegisterInvoice", "inv-1");
        var competitor = Assert.IsType<SemanticAccepted>(await Run(replicaRuntime, "IssueInvoice", "inv-1"));
        var competing = replicaLog.Facts[^1];

        await Register("inv-1");
        _log.BeforeAppend = () =>
        {
            _log.BeforeAppend = null;
            _log.Facts.Add(competing);
        };
        var result = await Execute("IssueInvoice", "inv-1").WaitAsync(_limit);

        var conflict = Assert.IsType<SemanticConflict>(result);
        conflict.Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
        _log.Facts.Count(fact => fact.EventContract == competitor.Facts.Single().EventContract).ShouldEqual(1);
        _log.Writes.ShouldEqual(1);
        ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
    }

    [Fact]
    public async Task should_not_reject_competing_commands_in_other_decision_scopes()
    {
        await Register("inv-1");
        await Register("inv-2");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _log.PauseBeforeAppend = () =>
        {
            _log.PauseBeforeAppend = null;
            entered.SetResult();
            return release.Task;
        };

        var first = Execute("IssueInvoice", "inv-1");
        await entered.Task.WaitAsync(_limit);
        var other = Execute("IssueInvoice", "inv-2");

        // The decision gate is runtime-wide: the other scope waits for the parked append instead of racing it.
        await Task.Delay(200);
        other.IsCompleted.ShouldBeFalse();
        release.SetResult();

        var one = Assert.IsType<SemanticAccepted>(await first.WaitAsync(_limit));
        var two = Assert.IsType<SemanticAccepted>(await other.WaitAsync(_limit));
        _log.Writes.ShouldEqual(4);
        IssuedCount(one).ShouldEqual(2);
        IssuedCount(two).ShouldEqual(2);
    }

    int IssuedCount(SemanticAccepted accepted) => _log.Facts.Count(fact => fact.EventContract == accepted.Facts.Single().EventContract);

    async Task Register(string id) => Assert.IsType<SemanticAccepted>(await Execute("RegisterInvoice", id));

    Task<SemanticExecutionResult> Execute(string name, string invoiceId) => Run(_runtime, name, invoiceId);

    Task<SemanticExecutionResult> Run(ISemanticRuntime runtime, string name, string invoiceId)
    {
        var command = _plan.Commands.Values.Single(command => command.Name == name);
        var json = JsonSerializer.SerializeToElement(new { invoiceId });
        var values = command.Properties.Where(property => !property.IsGenerated && json.TryGetProperty(property.Name, out _))
            .ToDictionary(property => property.Name, property => json.GetProperty(property.Name));

        return runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), false);
    }
}
