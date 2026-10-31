// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_discovering_outside_development;

public class in_the_docker_sandbox : given.a_sandbox_host
{
    HttpStatusCode _commandCatalog;
    HttpStatusCode _queryCatalog;
    HttpStatusCode _usersCatalog;
    HttpStatusCode _tenantsCatalog;
    HttpStatusCode _identityDetailsSchema;

    async Task Because()
    {
        _commandCatalog = await Get("/.cratis/commands");
        _queryCatalog = await Get("/.cratis/queries");
        _usersCatalog = await Get("/.cratis/users");
        _tenantsCatalog = await Get("/.cratis/tenants");
        _identityDetailsSchema = await Get("/.cratis/identity-details/schema");
    }

    [Fact] void should_require_authentication_for_the_command_catalog() => _commandCatalog.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_require_authentication_for_the_query_catalog() => _queryCatalog.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_require_authentication_for_the_users_catalog() => _usersCatalog.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_require_authentication_for_the_tenants_catalog() => _tenantsCatalog.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_require_authentication_for_the_identity_details_schema() => _identityDetailsSchema.ShouldEqual(HttpStatusCode.Unauthorized);
}
