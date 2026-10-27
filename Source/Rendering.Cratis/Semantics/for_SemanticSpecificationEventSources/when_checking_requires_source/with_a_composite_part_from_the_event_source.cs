// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

public class with_a_composite_part_from_the_event_source : a_source_observability_check
{
    void Because()
    {
        _model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        var slice = _model.Application.Modules.Single().Features.Single().Slices.Single();
        var projection = slice.Projections.Single();
        var scope = projection.Scope!;
        var contribution = scope.From[0];
        var key = (SemanticProjectionPartsKey)contribution.Key;
        var parts = key.Parts.SetItem(0, key.Parts[0] with { Value = new SemanticProjectionEventSourceIdentity() });
        _requiresSource = Check(slice with { Projections = [projection with { Scope = scope with { From = [contribution with { Key = new SemanticProjectionPartsKey(parts) }] } }] });
    }

    [Fact] void should_require_a_source_for_the_identity_part() => _requiresSource.ShouldBeTrue();
}
