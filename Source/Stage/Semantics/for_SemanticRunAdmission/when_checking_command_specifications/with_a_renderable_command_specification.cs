// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission.when_checking_command_specifications;

public class with_a_renderable_command_specification : given.a_run_specification
{
    void Establish()
    {
        Prepare(Invoicing);
        _specification = _specification;
    }

    void Because() => Check();

    [Fact] void should_admit_it() => Refusal().ShouldEqual("<admitted>");
}
