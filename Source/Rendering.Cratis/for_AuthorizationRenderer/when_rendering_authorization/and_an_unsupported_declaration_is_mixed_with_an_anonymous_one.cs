// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_an_unsupported_declaration_is_mixed_with_an_anonymous_one : an_application_with_policies
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => RenderAll(null, Authorize("Owner")));

    [Fact] void should_block_the_artifact_despite_the_anonymous_declaration() =>
        _error.ShouldBeOfExactType<AuthorizationCannotBeRendered>();
    [Fact] void should_report_that_the_portable_policy_backend_is_required() =>
        _error.Message.ShouldContain("STAGE-AUTH-001:");
}
#endif
