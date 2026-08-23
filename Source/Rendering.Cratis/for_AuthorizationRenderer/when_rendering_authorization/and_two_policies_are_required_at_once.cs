// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

/// <summary>
/// Specifies that <c>authorize Administrator and Auditor</c> is a conjunction and must never be weakened into
/// Arc's role disjunction.
/// </summary>
public class and_two_policies_are_required_at_once : an_application_with_policies
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => Render(AuthorizeAll("Administrator", "Auditor")));

    [Fact] void should_block_the_artifact() => _error.ShouldBeOfExactType<AuthorizationCannotBeRendered>();
    [Fact] void should_report_that_the_portable_policy_backend_is_required() =>
        _error.Message.ShouldEqual(
            "STAGE-AUTH-001: Command 'RegisterInvoice' declares a conjunction, which Arc's role attribute would weaken to a disjunction. " +
            "The artifact was not rendered because faithful authorization requires the future Screenplay-owned portable policy backend.");
}
#endif
