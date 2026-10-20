// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class with_sandbox_disabled : given.a_started_multimodule_host
{
    protected override bool SandboxEnabled => false;

    async Task Because() => await Send("GET", "/api/catalog/browse/catalog-lookup/catalog-items");

    [Fact] void should_not_supply_a_persona() => _persona.ShouldEqual("anonymous");
    [Fact] void should_deny_the_query() => _status.ShouldEqual(StatusCodes.Status403Forbidden);
}
