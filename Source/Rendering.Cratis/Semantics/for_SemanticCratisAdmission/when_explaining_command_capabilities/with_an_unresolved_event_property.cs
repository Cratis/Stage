// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_unresolved_event_property : given.a_command_model
{
    void Establish()
    {
        var @event = _slice.Events.Single();
        var property = @event.Properties.Single();
        _slice = _slice with { Events = [@event with { Properties = [property with { Type = property.Type with { Primitive = SemanticPrimitiveType.Unknown } }] }] };
    }

    void Because() => Evaluate();

    [Fact] void should_name_the_event_property_and_type_fix() => Messages.ShouldContainOnly(["Command 'IssueInvoice' event 'InvoiceIssued' property 'description' has an unresolved type; Stage cannot emit its C# type. Declare a supported primitive, concept, or composite type."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
