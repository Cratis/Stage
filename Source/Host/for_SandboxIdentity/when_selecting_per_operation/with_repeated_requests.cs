// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class with_repeated_requests : given.a_started_multimodule_host
{
    async Task Because()
    {
        await Send("GET", "/api/catalog/browse/catalog-lookup/catalog-items");
        await Send("GET", "/api/storefront/browse/shop-lookup/shop-items");
        await Send("GET", "/api/catalog/browse/catalog-lookup/catalog-items");
    }

    [Fact] void should_select_the_same_persona_again() => _persona.ShouldEqual("persona:Merchandiser");
    [Fact] void should_not_mutate_the_cached_principal() => _hasMutation.ShouldBeFalse();
    [Fact] void should_authorize_the_query() => _status.ShouldEqual(StatusCodes.Status200OK);
}
