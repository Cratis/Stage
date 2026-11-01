// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_one_of_several_declarations_is_missing : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = RenderAll(Authorize("Administrator"), null);

    [Fact] void should_follow_the_declaration_that_asks_for_nothing() => _attribute.ShouldEqual("AllowAnonymous");
}
