// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_cross_stream_destination : a_command_only_plan
{
    SemanticTypeReference _type = null!;
    SemanticTypeReference _expected = null!;

    void Because()
    {
        var command = _plan.Commands[_specification.When!.Command];
        var property = command.Properties.Single(candidate => candidate.Type != command.Destination!.Type);
        var produced = command.Produces[0] with { Destination = new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, property.Id) };
        var context = new SemanticRunContext(typeof(DynamicCommand), command, _specification, new(), new SemanticRuntimeTypes(_plan), new EventStoreForTesting().EventLog, _plan.Model.SemanticVersion);
        _type = context.Produce(produced).Fact.EventSource!.Type;
        _expected = property.Type;
    }

    [Fact] void should_use_the_override_property_type() => _type.ShouldEqual(_expected);
}
