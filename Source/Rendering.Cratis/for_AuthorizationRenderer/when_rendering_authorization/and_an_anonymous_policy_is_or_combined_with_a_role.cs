// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_an_anonymous_policy_is_or_combined_with_a_role : an_application_with_policies
{
    string? _attribute;

    void Because() => _attribute = Render(Authorize("Public", "Administrator"));

    [Fact] void should_allow_anonymous_access_explicitly() => _attribute.ShouldEqual("AllowAnonymous");
}
