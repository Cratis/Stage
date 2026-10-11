// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using OneOf;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_multiple_integer_parts : given.a_routed_command
{
    readonly List<ValidationResult> _results = [];

    void Establish()
    {
        var month = _command.Properties[1];
        var other = month with { Id = SemanticId.Parse($"sem1:{new string('d', 64)}"), Name = "otherMonth" };
        _source = _source with { Streams = [_source.Streams[0] with { StreamIdType = null, StreamIdParts = [new("other", other.Type), new("month", month.Type)] }] };
        _command = _command with
        {
            Properties = [.. _command.Properties, other],
            Route = _command.Route! with
            {
                StreamId = null,
                StreamIdParts = [new("other", SemanticExpression.Property(SemanticExpressionRootKind.Command, other.Id)), new("month", _command.Route.StreamId!)]
            }
        };
    }

    void Because()
    {
        Plan();
        var assembly = Load();
        var account = Activator.CreateInstance(assembly.GetType("InvoiceApp.Common.AccountId")!, Guid.NewGuid())!;
        var monthType = assembly.GetType("InvoiceApp.Common.Month")!;
        var commandType = assembly.GetType("InvoiceApp.Banking.Deposits.Deposit.Deposit")!;
        foreach (var other in new[] { 9007199254740992L, 42L })
        {
            var command = Activator.CreateInstance(commandType, account, Activator.CreateInstance(monthType, 9007199254740992L)!, 42m, Activator.CreateInstance(monthType, other)!)!;
            _results.Add(Assert.IsType<ValidationResult>(((IOneOf)commandType.GetMethod("Handle")!.Invoke(command, null)!).Value));
        }
    }

    [Fact] void should_report_the_first_invalid_declared_part_not_the_first_command_property() => _results[0].Members.ShouldContainOnly(["OtherMonth"]);
    [Fact] void should_validate_later_parts_before_returning_events() => _results[1].Members.ShouldContainOnly(["Month"]);
}
