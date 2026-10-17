// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_protected_command_reads : Specification
{
    SemanticExecutionPlan _plan = null!;
    ISemanticRuntime _runtime = null!;
    a_semantic_fact_log _log = null!;

    void Establish()
    {
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan())).Plan!;
        _log = new();
        _runtime = _log.Runtime(_plan, SemanticWorld.Empty);
    }

    void Destroy() => ((IDisposable)_runtime).Dispose();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_report_required_absence_before_requirements_or_generation(bool validateOnly)
    {
        foreach (var name in new[] { "Consume", "RequireApproval", "AllocateReceipt" })
        {
            var outcome = await Execute(name, new { id = "target", sourceId = "missing", approved = false }, validateOnly);
            var rejected = Assert.IsType<SemanticRejected>(outcome);
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
            rejected.Details.ShouldEqual("Current 'missing' does not exist.");
            rejected.World.Facts.ShouldBeEmpty();
        }
        _log.Attempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_reject_a_read_path_requirement_after_presence()
    {
        await Store("");
        var rejected = Assert.IsType<SemanticRejected>(await Execute("Consume", new { id = "target", sourceId = "reference" }));
        rejected.Details.ShouldEqual("The stored value must not be empty.");
        rejected.World.Facts.Length.ShouldEqual(1);
    }

    [Fact]
    public async Task should_map_the_read_value_across_sources()
    {
        await Store("original");
        var accepted = Assert.IsType<SemanticAccepted>(await Execute("Consume", new { id = "target", sourceId = "reference" }));
        accepted.Facts.Single().Destination.ShouldEqual(SemanticValue.Text("target"));
        accepted.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("original"));
        accepted.World.Facts.Length.ShouldEqual(2);
    }

    [Fact]
    public async Task should_run_command_only_requirements_after_presence()
    {
        await Store("present");
        var rejected = Assert.IsType<SemanticRejected>(await Execute("RequireApproval", new { id = "target", sourceId = "reference", approved = false }));
        rejected.Details.ShouldEqual("Approval required.");
        rejected.World.Facts.Length.ShouldEqual(1);
    }

    [Fact]
    public async Task should_generate_only_after_required_presence()
    {
        await Store("present");
        var accepted = Assert.IsType<SemanticAccepted>(await Execute("AllocateReceipt", new { id = "target", sourceId = "reference" }));
        var receipt = Assert.IsType<SemanticScalarExecutionResponse>(accepted.Response);
        Guid.Parse(((SemanticTextValue)receipt.Value).Value).ShouldNotEqual(Guid.Empty);
        accepted.World.Facts.Length.ShouldEqual(1);
    }

    [Fact]
    public async Task should_preserve_an_irrelevant_outside_append_without_faulting()
    {
        await Store("original");
        var destination = SemanticValue.Text("unrelated");
        _log.Facts.Add(_log.Facts.Single() with
        {
            Destination = destination,
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), destination))
        });
        var accepted = Assert.IsType<SemanticAccepted>(await Execute("Consume", new { id = "target", sourceId = "reference" }));
        accepted.World.Facts.Length.ShouldEqual(3);
        accepted.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("original"));
        ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
        _log.Attempts[^1].Tail.ShouldEqual(1UL);
        (await Execute("Consume", new { id = "another", sourceId = "reference" })).ShouldBeOfExactType<SemanticAccepted>();
    }

    [Fact]
    public async Task should_conflict_only_the_stale_decision_and_allow_a_new_one()
    {
        await Store("original");
        _log.Facts.Add(_log.Facts.Single() with
        {
            Values = [new(_log.Facts.Single().Values.Single().TargetProperty, SemanticValue.Text("changed"))]
        });
        var conflict = Assert.IsType<SemanticConflict>(await Execute("Consume", new { id = "target", sourceId = "reference" }));
        conflict.Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
        conflict.World.Facts.Length.ShouldEqual(2);
        _log.Writes.ShouldEqual(1);
        var next = Assert.IsType<SemanticAccepted>(await Execute("Consume", new { id = "target", sourceId = "reference" }));
        next.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("changed"));
        ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_recommit_the_same_decision_after_a_definite_guard_loss(bool relevant)
    {
        await Store("original");
        _log.BeforeAppend = () =>
        {
            _log.BeforeAppend = null;
            var destination = SemanticValue.Text(relevant ? "reference" : "other");
            _log.Facts.Add(_log.Facts.Single() with
            {
                Destination = destination,
                Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), destination))
            });
        };
        var result = await Execute("Consume", new { id = "target", sourceId = "reference" });
        if (relevant)
        {
            result.ShouldBeOfExactType<SemanticConflict>();
            _log.Attempts.Count.ShouldEqual(2);
        }
        else
        {
            result.ShouldBeOfExactType<SemanticAccepted>();
            _log.Attempts.Count.ShouldEqual(3);
            _log.Attempts[^1].Tail.ShouldEqual(1UL);
            _log.Attempts[^1].Facts.Single().Values.Single().ShouldEqual(_log.Attempts[^2].Facts.Single().Values.Single());
        }
        ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
    }

    [Fact]
    public void should_defensively_map_a_reference_conflict_to_concurrency()
    {
        var conflict = new SemanticConflict(SemanticWorld.Empty, SemanticConflictCategory.DecisionStateChanged, "Decision state changed.");
        var result = SemanticCommandOutcome.Map(conflict, CorrelationId.New(), null, "Consume");
        result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
        result.ValidationResults.Single().Message.ShouldEqual(conflict.Details);
    }

    async Task Store(string value) => Assert.IsType<SemanticAccepted>(await Execute("Store", new { id = "reference", value }));

    Task<SemanticExecutionResult> Execute(string name, object payload, bool validateOnly = false)
    {
        var command = _plan.Commands.Values.Single(command => command.Name == name);
        var json = JsonSerializer.SerializeToElement(payload);
        var values = command.Properties.Where(property => !property.IsGenerated && json.TryGetProperty(property.Name, out _))
            .ToDictionary(property => property.Name, property => json.GetProperty(property.Name));

        return _runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), validateOnly);
    }
}
