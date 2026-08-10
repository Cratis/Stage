// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

public class and_the_policy_is_authored_code : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render(Authorize("Bespoke"));

    [Fact] void should_fall_back_to_requiring_an_authenticated_caller() => _attribute.ShouldEqual("Authorize");
    [Fact] void should_report_the_code_block_as_unrenderable() =>
        _diagnostics.ShouldContain(
            "Command 'RegisterInvoice' authorizes against policy 'Bespoke', whose requirement is a csharp block with no attribute " +
            "equivalent — rendered as requiring an authenticated caller.");
}
