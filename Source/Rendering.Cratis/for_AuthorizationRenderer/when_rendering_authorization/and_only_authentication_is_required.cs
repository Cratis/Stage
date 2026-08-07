// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_only_authentication_is_required : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render(Authorize("Authenticated"));

    [Fact] void should_require_an_authenticated_caller() => _attribute.ShouldEqual("Authorize");
    [Fact] void should_not_report_anything() => _diagnostics.ShouldBeEmpty();
}
