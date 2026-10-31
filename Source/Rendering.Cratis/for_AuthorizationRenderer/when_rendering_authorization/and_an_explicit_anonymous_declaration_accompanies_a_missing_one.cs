// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_an_explicit_anonymous_declaration_accompanies_a_missing_one : an_application_with_policies
{
    string? _attribute;

    void Because() => _attribute = Render(Authorize("Public"), null);

    [Fact] void should_preserve_the_explicit_anonymous_opt_in() => _attribute.ShouldEqual("AllowAnonymous");
}
