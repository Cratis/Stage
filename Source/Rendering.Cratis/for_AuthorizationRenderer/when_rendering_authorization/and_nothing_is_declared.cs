// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_nothing_is_declared : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render(null);

    [Fact] void should_require_authentication() => _attribute.ShouldEqual("Authorize");
    [Fact]
    void should_report_the_inferred_default() => _diagnostics.ShouldContain(
        "Command 'RegisterInvoice' requires an authenticated caller because the model has no authorize clause; " +
        "Stage renders an authenticated caller as the default, unlike Screenplay's reference semantics where a missing clause allows everyone.");
}
