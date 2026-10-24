// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using OneOf;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_integer_stream_id_bounds : given.a_routed_command
{
    readonly List<(long Value, bool Composite, object Result)> _accepted = [];
    readonly List<(long Value, object Result)> _rejected = [];
    readonly List<Exception> _exceptions = [];

    void Because()
    {
        Exercise(false);
        var month = _command.Properties[1];
        _source = _source with { Streams = [_source.Streams[0] with { StreamIdType = null, StreamIdParts = [new("count", month.Type), new("label", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))] }] };
        _command = _command with
        {
            Route = _command.Route! with
            {
                StreamId = null,
                StreamIdParts = [new("count", _command.Route.StreamId!), new("label", SemanticExpression.FromValue(SemanticValue.Text("b")))]
            },
            Produces = [_command.Produces[0], _command.Produces[0]]
        };
        Exercise(true);
    }

    void Exercise(bool composite)
    {
        Plan();
        var assembly = Load();
        var account = Activator.CreateInstance(assembly.GetType("InvoiceApp.Common.AccountId")!, Guid.NewGuid())!;
        var monthType = assembly.GetType("InvoiceApp.Common.Month")!;
        var commandType = assembly.GetType("InvoiceApp.Banking.Deposits.Deposit.Deposit")!;
        foreach (var value in new[] { long.MinValue, -9007199254740993L, -9007199254740992L, -9007199254740991L, -9007199254740990L, 9007199254740990L, 9007199254740991L, 9007199254740992L, 9007199254740993L, long.MaxValue })
        {
            var command = Activator.CreateInstance(commandType, account, Activator.CreateInstance(monthType, value)!, 42m)!;
            var exception = Catch.Exception(() =>
            {
                var result = commandType.GetMethod("Handle")!.Invoke(command, null)!;
                if (value is >= -9007199254740991L and <= 9007199254740991L) _accepted.Add((value, composite, result));
                else _rejected.Add((value, result));
            });
            if (exception is not null) _exceptions.Add(exception);
        }
    }

    [Fact] void should_never_throw_for_an_integer_identity() => _exceptions.ShouldBeEmpty();
    [Fact] void should_reject_every_out_of_range_scalar_and_composite_before_returning_events() => _rejected.TrueForAll(rejected => rejected.Result is IOneOf { Value: ValidationResult }).ShouldBeTrue();
    [Fact] void should_check_all_rejected_values() => _rejected.Count.ShouldEqual(12);
    [Fact]
    void should_return_value_free_errors_naming_the_property()
    {
        foreach (var (value, result) in _rejected)
        {
            var validation = Assert.IsType<ValidationResult>(Assert.IsType<IOneOf>(result, exactMatch: false).Value);
            validation.Severity.ShouldEqual(ValidationResultSeverity.Error);
            validation.Members.ShouldContainOnly(["month"]);
            validation.Message.ShouldNotContain(value.ToString(CultureInfo.InvariantCulture));
        }
    }
    [Fact]
    void should_accept_the_endpoints_and_their_neighbors_with_exact_ids()
    {
        _accepted.Count.ShouldEqual(8);
        foreach (var (value, composite, result) in _accepted)
        {
            var payload = Assert.IsType<IOneOf>(result, exactMatch: false).Value;
            var events = composite ? Assert.IsType<IEnumerable<EventForEventSourceId>>(payload, exactMatch: false) : [Assert.IsType<EventForEventSourceId>(payload)];
            events.All(@event => @event.EventStreamId.Value == value.ToString(CultureInfo.InvariantCulture) + (composite ? "|b" : string.Empty)).ShouldBeTrue();
        }
    }
}
