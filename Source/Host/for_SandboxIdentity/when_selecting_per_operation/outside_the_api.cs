// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class outside_the_api : given.a_started_multimodule_host
{
    async Task Because() => await Send("GET", "/outside");

    [Fact] void should_use_the_expected_persona() => _persona.ShouldEqual("anonymous");
    [Fact] void should_return_the_expected_status() => _status.ShouldEqual(StatusCodes.Status200OK);
}
