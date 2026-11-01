// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

public class with_composite_parts_from_event_properties : a_source_observability_check
{
    void Because()
    {
        _model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        _requiresSource = Check(_model.Application.Modules.Single().Features.Single().Slices.Single());
    }

    [Fact] void should_not_require_a_source_for_the_parts_key() => _requiresSource.ShouldBeFalse();
}
