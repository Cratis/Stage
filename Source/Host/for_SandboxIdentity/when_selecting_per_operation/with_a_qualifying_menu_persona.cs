// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class with_a_qualifying_menu_persona : given.a_started_multimodule_host
{
    async Task Because() => await Send("GET", "/api/storefront/browse/shop-lookup/shop-items");

    [Fact] void should_keep_the_menu_persona_over_an_earlier_qualifying_persona() => _persona.ShouldEqual("persona:Shopper");
    [Fact] void should_keep_the_menu_display_name() => _displayName.ShouldEqual("Sandbox Shopper");
    [Fact] void should_authorize_the_query() => _status.ShouldEqual(StatusCodes.Status200OK);
}
