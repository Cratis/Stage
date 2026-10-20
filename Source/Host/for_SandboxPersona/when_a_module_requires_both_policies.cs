// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_a_module_requires_both_policies : given.a_persona_model
{
    const string Source = """
        policy IsStaff
          require role "Staff"
        policy IsManager
          require role "Manager"
        persona Staff
          policy IsStaff
        persona Manager
          policy IsStaff
          policy IsManager
        module Office
          authorize IsStaff and IsManager
        """;

    void Because() => _principal = Create(Source);

    [Fact] void should_use_the_persona_holding_both() => _principal!.FindFirstValue("sub").ShouldEqual("persona:Manager");
}
