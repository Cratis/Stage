// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class without_mirror_read_model_pages : a_stored_sequence
{
    SemanticWorld _world = null!;

    async Task Because() => _world = await SemanticChronicleRegistration.Rebuild(_accessor, "Projects", _plan, 0);

    [Fact] void should_project_the_event_without_chronicle_read_model_pages() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
    [Fact] void should_not_request_mirror_pages() => _ = _services.ReadModels.DidNotReceive().GetInstances(Arg.Any<GetInstancesRequest>());
}
