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

    [Fact] void should_state_the_absence_as_anonymous_access() => _attribute.ShouldEqual("AllowAnonymous");
    [Fact] void should_not_report_anything() => _diagnostics.ShouldBeEmpty();
}
