// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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

public class with_composite_read_model_keys : Specification
{
    SemanticExecutionPlan _plan = null!;
    ISemanticRuntime _runtime = null!;
    SemanticReadModel _readModel = null!;
    ImmutableArray<SemanticReadModelInstance> _instances;
    SemanticExecutionResult _byParts = null!;
    SemanticExecutionResult _removedByParts = null!;
    SemanticExecutionResult _invalidPart = null!;
    SemanticExecutionResult _filtered = null!;

    async Task Establish()
    {
        _plan = compiled_plan.From(composite_key_payments.Source);
        _readModel = _plan.ReadModels.Values.Single();
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var facts = new List<SemanticFact>();
        ((ISemanticFactTail)appender).Tail().Returns(_ => facts.Count == 0 ? ulong.MaxValue : (ulong)facts.Count - 1);
        appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(facts.AddRange), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
        _runtime = SemanticRuntimeHosting.Create(_plan, appender);
        await Execute("ReceivePayment", "payment-1", "INV-1", "card", 12);
        await Execute("ReceivePayment", "payment-2", "INV-1", "cash", 8);
        await Execute("ReceivePayment", "payment-3", "INV-2", "card", 3);
        await Execute("RemovePayment", "payment-1", "INV-1", "card");
    }

    async Task Because()
    {
        var total = _plan.Queries.Values.Single(query => query.Name == "Total");
        var list = _plan.Queries.Values.Single(query => query.Name == "TotalsForInvoice");
        _instances = await _runtime.ReadModels(_readModel.Id);
        _byParts = await _runtime.Query(total, Key("INV-1", "cash"), new ClaimsPrincipal());
        _removedByParts = await _runtime.Query(total, Key("INV-1", "card"), new ClaimsPrincipal());
        _invalidPart = await _runtime.Query(total, Key("X", "card"), new ClaimsPrincipal());
        _filtered = await _runtime.Query(list, SemanticValue.Null, new ClaimsPrincipal(), [new(list.Filters.Single(filter => filter.Name == "invoiceId").Id, SemanticValue.Text("INV-1"))]);
    }

    [Fact] void should_compile_at_v27() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V27);
    [Fact] void should_admit_the_model_in_the_host() => SemanticRunAdmission.ModelFeatures(_plan.Model).ShouldBeEmpty();
    [Fact] void should_have_no_blocking_runtime_refusal() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_keep_instances_that_differ_in_one_part_apart() => Totals(_instances).ShouldContainOnly(("INV-1", "cash", 8m), ("INV-2", "card", 3m));
    [Fact] void should_answer_a_query_by_parts() => Totals(((SemanticAccepted)_byParts).Queries.Single().Results).ShouldContainOnly(("INV-1", "cash", 8m));
    [Fact] void should_remove_only_the_instance_whose_parts_all_match() => ((SemanticAccepted)_removedByParts).Queries.Single().Results.ShouldBeEmpty();
    [Fact] void should_reject_an_invalid_part_value() => ((SemanticRejected)_invalidPart).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_filter_a_list_query_on_one_part() => Totals(((SemanticAccepted)_filtered).Queries.Single().Results).ShouldContainOnly(("INV-1", "cash", 8m));

    SemanticValue Key(string invoiceId, string method) => SemanticValue.Composite(
    [
        new(_readModel.Properties.Single(property => property.Name == "invoiceId").Id, SemanticValue.Text(invoiceId)),
        new(_readModel.Properties.Single(property => property.Name == "method").Id, SemanticValue.Text(method))
    ]);

    IEnumerable<(string Invoice, string Method, decimal Total)> Totals(IEnumerable<SemanticReadModelInstance> instances) =>
        instances.Select(instance => (Text(instance, "invoiceId"), Text(instance, "method"), ((SemanticNumberValue)Value(instance, "total")).Value));

    string Text(SemanticReadModelInstance instance, string property) => ((SemanticTextValue)Value(instance, property)).Value;

    SemanticValue Value(SemanticReadModelInstance instance, string property) =>
        instance.Values.Single(value => value.TargetProperty == _readModel.Properties.Single(candidate => candidate.Name == property).Id).Value;

    async Task Execute(string command, string paymentId, string invoice, string method, int? amount = null)
    {
        var target = _plan.Commands.Values.Single(candidate => candidate.Name == command);
        var payload = new Dictionary<string, JsonElement>
        {
            ["paymentId"] = JsonSerializer.SerializeToElement(paymentId),
            ["invoice"] = JsonSerializer.SerializeToElement(invoice),
            ["method"] = JsonSerializer.SerializeToElement(method)
        };
        if (amount is { } value)
        {
            payload["amount"] = JsonSerializer.SerializeToElement(value);
        }

        var outcome = await _runtime.Execute(target, payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), false);
        outcome.ShouldBeOfExactType<SemanticAccepted>();
    }
}
