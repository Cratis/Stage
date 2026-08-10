// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_several_declarations_are_combined : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = RenderAll(Authorize("Administrator"), Authorize("Auditor"));

    [Fact] void should_permit_everyone_any_one_of_them_permits() => _attribute.ShouldEqual("Roles(\"Administrator\", \"Auditor\")");
}
