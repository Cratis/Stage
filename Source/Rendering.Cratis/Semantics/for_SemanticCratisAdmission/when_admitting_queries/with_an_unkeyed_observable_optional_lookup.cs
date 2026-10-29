// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_an_unkeyed_observable_optional_lookup : given.a_query_shape
{
    Exception _failure = null!;

    void Establish() => Configure(SemanticQueryCardinality.ZeroOrOne, SemanticQueryDelivery.Live, false);
    void Because() => _failure = Catch.Exception(() => Context());

    [Fact]
    void should_refuse_the_query_shape_before_stage_admission()
    {
        _failure.ShouldBeOfExactType<InvalidSemanticContract>();
        _failure.Message.ShouldContain("An unkeyed query must return a collection.");
    }
}
