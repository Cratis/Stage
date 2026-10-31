// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_the_policy_requires_anonymous : an_application_with_policies
{
    string? _attribute;

    void Because() => _attribute = Render(Authorize("Public"));

    [Fact] void should_allow_anonymous_access_explicitly() => _attribute.ShouldEqual("AllowAnonymous");
    [Fact] void should_not_report_anything() => _diagnostics.ShouldBeEmpty();
}
