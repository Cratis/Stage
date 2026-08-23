// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_the_policy_is_declared_nowhere : an_application_with_policies
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => Render(Authorize("Nonexistent")));

    [Fact] void should_block_the_artifact() => _error.ShouldBeOfExactType<AuthorizationCannotBeRendered>();
    [Fact] void should_report_that_nothing_declares_it() =>
        _error.Message.ShouldEqual(
            "STAGE-AUTH-001: Command 'RegisterInvoice' references policy 'Nonexistent', which nothing declares. The artifact was not rendered because " +
            "faithful authorization requires the future Screenplay-owned portable policy backend.");
}
#endif
