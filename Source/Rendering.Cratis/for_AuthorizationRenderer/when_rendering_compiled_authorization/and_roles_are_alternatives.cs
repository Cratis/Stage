// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_compiled_authorization;

public class and_roles_are_alternatives : Specification
{
    const string Source =
        """
        policy Administrator
          require role "Administrator"

        policy Auditor
          require role "Auditor"

        module Billing
          feature Invoicing
            slice StateChange Register
              command RegisterInvoice
                authorize Administrator or Auditor
        """;

    ApplicationSyntax _application = null!;
    string _attribute = null!;
    List<string> _diagnostics = null!;

    void Establish()
    {
        _application = new ScreenplayCompiler().Compile(Source).Value!;
        _diagnostics = [];
    }

    void Because()
    {
        var command = _application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        _attribute = AuthorizationRenderer.Render(
            command.Authorize,
            new ApplicationSet([_application]),
            $"Command '{command.Name}'",
            _diagnostics);
    }

    [Fact] void should_preserve_the_exact_role_disjunction() =>
        _attribute.ShouldEqual("Roles(\"Administrator\", \"Auditor\")");
    [Fact] void should_not_report_anything() => _diagnostics.ShouldBeEmpty();
}
#endif
