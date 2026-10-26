// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_discovering_outside_development;

public class in_the_docker_sandbox : given.a_sandbox_host
{
    [Fact] void should_require_authentication_for_the_command_catalog() => _commandCatalog.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_require_authentication_for_the_query_catalog() => _queryCatalog.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_answer_the_identity_of_the_page_as_the_persona() => _identityOfThePage.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_authorize_a_role_gated_api_request_as_the_persona() => _roleGatedApi.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_give_the_persona_to_requests_outside_the_api() => _roleGatedApiWithoutPersona.ShouldEqual(HttpStatusCode.Unauthorized);
}
