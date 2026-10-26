// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_discovering_outside_development;

public class in_development : given.a_sandbox_host
{
    protected override string Environment => "Development";

    [Fact] void should_serve_the_command_catalog_anonymously() => _commandCatalog.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_query_catalog_anonymously() => _queryCatalog.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_users_catalog_anonymously() => _usersCatalog.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_tenants_catalog_anonymously() => _tenantsCatalog.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_identity_details_schema_anonymously() => _identityDetailsSchema.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_answer_the_identity_of_the_page_as_the_persona() => _identityOfThePage.ShouldEqual(HttpStatusCode.OK);
}
