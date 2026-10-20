// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_loading_the_same_model_in_new_sessions : given.a_persona_model
{
    ClaimsPrincipal[] _sessions = [];

    void Because() => _sessions = [.. Enumerable.Range(0, 4).Select(_ => Create(Personas + "\n" + Menu)!)];

    [Fact] void should_select_the_same_persona_in_every_session() => _sessions.All(session => session.FindFirstValue("sub") == "persona:Shopper").ShouldBeTrue();
    [Fact] void should_synthesize_identical_claims_in_every_session() => _sessions.All(session => session.Claims.Select(claim => (claim.Type, claim.Value)).SequenceEqual(_sessions[0].Claims.Select(claim => (claim.Type, claim.Value)))).ShouldBeTrue();
    [Fact] void should_synthesize_identical_authentication_in_every_session() => _sessions.All(session => session.Identity!.AuthenticationType == "stage-persona").ShouldBeTrue();
}
