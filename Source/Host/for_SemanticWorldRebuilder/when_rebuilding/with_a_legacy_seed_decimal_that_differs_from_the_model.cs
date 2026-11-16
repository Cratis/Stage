// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_legacy_seed_decimal_that_differs_from_the_model : a_stored_seed_decimal
{
    Exception? _error;

    void Establish() => PrepareSeed("0.3", exactSchema: false);

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0, evidence: EvidenceFor(Group(0.30000000000000001m))));

    [Fact] void should_refuse_the_stored_seed_event() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
