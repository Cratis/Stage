// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_a_module_authorizes_a_later_persona : given.a_persona_model
{
    const string Module = """
        module Shop
          authorize IsShopper
        """;

    void Because() => _principal = Create(Personas + "\n" + Module);

    [Fact] void should_use_the_persona_the_module_admits() => _principal!.FindFirstValue("sub").ShouldEqual("persona:Shopper");
    [Fact] void should_carry_its_role() => _principal!.IsInRole("Shopper").ShouldBeTrue();
}
