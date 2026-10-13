// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_omitted_required_value : a_stored_partial_event
{
    Exception? _error;

    void Establish() => Prepare("String");
    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Fact(_partialPlan, _stored));

    [Fact] void should_refuse_the_incomplete_event() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
