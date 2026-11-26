// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_invocation_mappings;

public class and_an_ordered_list_is_reversed : Specification
{
    [Theory]
    [InlineData("invokes")]
    [InlineData("triggers")]
    [InlineData("produces")]
    [InlineData("projection mappings")]
    void should_remain_order_sensitive(string owner)
    {
        var expected = round_trip_invocation_mappings.Normalize(invocation_mapping_models.Ordered(owner, reverse: false));
        var actual = round_trip_invocation_mappings.Normalize(invocation_mapping_models.Ordered(owner, reverse: true));

        SemanticModelEquivalence.Compare(expected, actual).IsEquivalent.ShouldBeFalse();
    }
}
#endif
