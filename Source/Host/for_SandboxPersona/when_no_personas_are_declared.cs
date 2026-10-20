// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_no_personas_are_declared : given.a_persona_model
{
    void Because() => _principal = Create("concept Name : String");

    [Fact] void should_not_invent_an_identity() => _principal.ShouldBeNull();
}
