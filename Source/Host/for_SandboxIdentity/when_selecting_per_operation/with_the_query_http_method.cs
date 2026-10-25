// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class with_the_query_http_method : given.a_started_multimodule_host
{
    async Task Because() => await Send("QUERY", "/api/catalog/browse/catalog-lookup/catalog-items", "{}");

    [Fact] void should_use_the_expected_persona() => _persona.ShouldEqual("persona:Merchandiser");
    [Fact] void should_return_the_expected_status() => _status.ShouldEqual(StatusCodes.Status200OK);
}
