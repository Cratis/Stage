// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_a_model_declares_no_seeds : a_seeding_store
{
    SemanticWorld _world = null!;

    void Establish() => Prepare(string.Empty);

    async Task Because() => _world = await SemanticChronicleRegistration.Register(_client, Store, _plan, _seeds);

    [Fact] void should_append_nothing() => _appends.ShouldBeEmpty();
    [Fact] void should_start_with_an_empty_world() => _world.Facts.ShouldBeEmpty();
}
