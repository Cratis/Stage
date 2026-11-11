// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission.when_checking_command_specifications;

public class with_a_role_uri_claim : given.a_run_specification
{
    void Establish()
    {
        Prepare(Invoicing);
        _specification = _specification with { GivenCaller = new(true, [], [new(ClaimTypes.Role, "admin")]) };
    }

    void Because() => Check();

    [Fact] void should_refuse_the_authorization() => Refusal().ShouldEqual($"Authorization|{_specification.Id}|Role-URI claim types cannot be used as claims; roles and claims are separate in Screenplay.");
}
