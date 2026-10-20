// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding;

public class when_querying_as_the_sandbox_persona : given.a_persona_bound_query
{
    async Task Because() => (_status, _, _) = await Request("GET", "/api/projects/lookup/project-lookup/projects");

    [Fact] void should_authorize_the_role_gated_query_without_headers() => _status.ShouldEqual(StatusCodes.Status200OK);
}
