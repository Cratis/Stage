// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_invocation_mappings;

public class and_assignments_are_reordered : Specification
{
    NormalizedSemanticModel _expected;
    NormalizedSemanticModel _actual;
    SemanticModelComparison _comparison;

    void Establish()
    {
        _expected = invocation_mapping_models.Model(invocation_mapping_models.Assignments);
        _actual = invocation_mapping_models.Model(invocation_mapping_models.Assignments.Reverse());
    }

    void Because() => _comparison = SemanticModelEquivalence.Compare(round_trip_invocation_mappings.Normalize(_expected), round_trip_invocation_mappings.Normalize(_actual));

    [Fact] void should_compare_by_target_in_the_round_trip() => _comparison.IsEquivalent.ShouldBeTrue();
    [Fact] void should_keep_the_general_comparer_order_strict() => SemanticModelEquivalence.Compare(_expected, _actual).IsEquivalent.ShouldBeFalse();
    [Fact] void should_not_mutate_the_original_lists() => _expected.Root.Members["reactions"].Members["Follow"].Members["triggers"].Items[0].Members["invokes"].Items[0].Members["mappings"].Kind.ShouldEqual(NormalizedSemanticNodeKind.List);
}
#endif
