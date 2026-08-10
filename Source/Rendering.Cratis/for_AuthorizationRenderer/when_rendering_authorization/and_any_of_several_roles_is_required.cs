// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_any_of_several_roles_is_required : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render(Authorize("Administrator", "Auditor"));

    [Fact] void should_require_any_one_of_them() => _attribute.ShouldEqual("Roles(\"Administrator\", \"Auditor\")");
    [Fact] void should_not_report_anything() => _diagnostics.ShouldBeEmpty();
}
