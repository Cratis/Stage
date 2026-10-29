// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class with_a_root_local_unknown_reference_kind : given.an_omission_context
{
    SemanticOwnerOmission? _omission;

    void Establish()
    {
        var owner = _registerProject.Events.Single();
        Replace(owner, "Properties", owner.Properties.Select(property => property with
        {
            Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 }
        }).ToImmutableArray());
        BuildContext();
    }

    void Because() => _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id));

    [Fact] void should_retain_the_global_incomplete_signal() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_record_the_lexical_event_and_type_subtree() => _index.Incompleteness.All(gap => gap.Owner == _registerProject.Events.Single().Id && gap.PropertyTypeOf == gap.Owner && gap.Kind == SemanticDependencyIncompletenessKind.UnknownVariant).ShouldBeTrue();
    [Fact] void should_not_ignore_the_gap_without_the_event_root() => SemanticOwnerOmission.Compute(_context, _index, new Dictionary<SemanticId, string>()).ShouldBeNull();
    [Fact] void should_prove_the_closure_outside_the_root_event_property_types() => (_omission is not null).ShouldBeTrue();
}
