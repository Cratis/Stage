// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class with_a_command : given.a_started_multimodule_host
{
    async Task Because() => await Send("POST", "/api/catalog/browse/rename/rename-item", """{"itemId":"11111111-1111-1111-1111-111111111111","name":"Renamed"}""");

    [Fact] void should_select_the_command_persona() => _persona.ShouldEqual("persona:Merchandiser");
    [Fact] void should_authorize_the_command() => _status.ShouldEqual(StatusCodes.Status200OK);
}
