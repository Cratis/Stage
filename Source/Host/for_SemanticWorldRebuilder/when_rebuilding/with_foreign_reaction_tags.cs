// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_foreign_reaction_tags : given.a_persisted_reaction_world
{
    Exception? _error;

    void Establish() => _history[1].Context.Tags = ["foreign"];

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, _history, 1));

    [Fact] void should_refuse_metadata_outside_the_production() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
