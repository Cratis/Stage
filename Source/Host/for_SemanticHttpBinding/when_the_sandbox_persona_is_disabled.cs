// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding;

public class when_the_sandbox_persona_is_disabled : given.a_persona_bound_query
{
    protected override bool Enabled => false;

    async Task Because() => (_status, _, _) = await Request("GET", "/api/projects/lookup/project-lookup/projects");

    [Fact] void should_deny_the_anonymous_role_gated_query() => _status.ShouldEqual(StatusCodes.Status403Forbidden);
}
