// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_the_policy_requires_a_claim : an_application_with_policies
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => Render(Authorize("Owner")));

    [Fact] void should_block_the_artifact() => _error.ShouldBeOfExactType<AuthorizationCannotBeRendered>();
    [Fact] void should_report_the_claim_as_unrenderable() =>
        _error.Message.ShouldEqual(
            "STAGE-AUTH-001: Command 'RegisterInvoice' references policy 'Owner', which requires the claim 'sub'. The artifact was not rendered because " +
            "faithful authorization requires the future Screenplay-owned portable policy backend.");
}
#endif
