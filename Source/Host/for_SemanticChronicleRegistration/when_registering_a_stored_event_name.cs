// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_a_stored_event_name : given.a_stored_event_name
{
    async Task Because() => await SemanticChronicleRegistration.Register(_client, "Compliance", _plan);

    [Fact] void should_register_under_the_pinned_name() => _registrations.Single().Types.Single().Type.Id.ShouldEqual("Original");
}
