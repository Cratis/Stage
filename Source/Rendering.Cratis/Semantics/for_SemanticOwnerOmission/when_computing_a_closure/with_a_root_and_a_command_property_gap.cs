// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class with_a_root_and_a_command_property_gap : given.an_omission_context
{
    SemanticOwnerOmission? _omission;

    void Establish()
    {
        var @event = _registerProject.Events.Single();
        var command = _registerProject.Commands.Single();
        Replace(@event, "Properties", @event.Properties.Select(property => property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } }).ToImmutableArray());
        Replace(command, "Properties", command.Properties.Select(property => property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } }).ToImmutableArray());
        BuildContext();
    }

    void Because() => _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id));

    [Fact] void should_keep_command_property_gaps_outside_the_event_carveout() => _omission.ShouldBeNull();
    [Fact] void should_also_refuse_without_roots() => SemanticOwnerOmission.Compute(_context, _index, new Dictionary<SemanticId, string>()).ShouldBeNull();
}
