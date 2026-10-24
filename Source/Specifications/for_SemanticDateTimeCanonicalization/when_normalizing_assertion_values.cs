// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications.Comparison;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

namespace Cratis.Stage.Specifications.for_SemanticDateTimeCanonicalization;

public class when_normalizing_assertion_values : Specification
{
    [Theory]
    [InlineData("2026-01-01T00:00:00Z", "2026-01-01T00:00:00.0000000Z")]
    [InlineData("2026-01-01T00:00:00+00:00", "2026-01-01T00:00:00.0000000Z")]
    [InlineData("2026-01-01T02:00:00.1234567+02:00", "2026-01-01T02:00:00.1234567+02:00")]
    [InlineData("2026-01-01T00:00:00.123Z", "2026-01-01T00:00:00.1230000Z")]
    public void should_use_the_reference_round_trip_form(string input, string expected) =>
        Normalize(input, SemanticPrimitiveType.DateTime).ShouldEqual(SemanticValue.Text(expected));

    [Theory]
    [InlineData(SemanticPrimitiveType.DateTime, "2026-01-01T00:00:00Z", "2026-01-01T00:00:00+00:00", true)]
    [InlineData(SemanticPrimitiveType.DateTime, "2026-01-01T02:00:00+02:00", "2026-01-01T00:00:00Z", false)]
    [InlineData(SemanticPrimitiveType.Text, "2026-01-01T00:00:00Z", "2026-01-01T00:00:00+00:00", false)]
    [InlineData(SemanticPrimitiveType.Text, "ordinary text", "ordinary text", true)]
    public void should_compare_canonical_values_ordinally(SemanticPrimitiveType primitive, string left, string right, bool equal) =>
        SemanticExpectationComparer.AreEqual(Normalize(left, primitive), Normalize(right, primitive)).ShouldEqual(equal);

    static SemanticValue Normalize(string text, SemanticPrimitiveType primitive)
    {
        var application = when_running_property_keyed_removal_only_projections.Compile("""
            module Scheduling
              feature Windows
                slice StateView Current
                  readmodel Current
                    id String key
            """).Model.Application;
        return SemanticDateTimeCanonicalization.Normalize(SemanticValue.Text(text), SemanticTypeReference.ForPrimitive(primitive), application);
    }
}
