// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class with_forged_identity_headers : given.a_started_multimodule_host
{
    async Task Because()
    {
        _client.DefaultRequestHeaders.Add("X-MS-CLIENT-PRINCIPAL", "spoofed");
        _client.DefaultRequestHeaders.Add("X-Ms-Client-Principal-Id", "attacker");
        _client.DefaultRequestHeaders.Add("x-ms-client-principal-name", "Attacker");
        _client.DefaultRequestHeaders.Add("X-MS-CLIENT-PRINCIPAL-IDP", "attacker");
        await Send("GET", "/api/catalog/browse/catalog-lookup/catalog-items?persona=SalesRep");
    }

    [Fact] void should_select_only_from_the_model_and_route() => _persona.ShouldEqual("persona:Merchandiser");
    [Fact] void should_authorize_the_catalog_query() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_invoke_authentication() => _recording.Invocations.ShouldNotBeEmpty();
    [Fact] void should_strip_headers_before_authentication() => _recording.Invocations.SelectMany(headers => headers).ShouldBeEmpty();
    [Fact] void should_strip_headers_before_the_endpoint() => _forwardedHeaders.ShouldBeEmpty();
}
