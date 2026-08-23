// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_compiled_authorization;

public class and_two_policies_are_required_at_once : Specification
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
                authorize Administrator and Auditor
        """;

    ApplicationSyntax _application = null!;
    Exception _error = null!;
    List<string> _diagnostics = null!;

    void Establish()
    {
        _application = new ScreenplayCompiler().Compile(Source).Value!;
        _diagnostics = [];
    }

    void Because()
    {
        var command = _application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        _error = Catch.Exception(() => AuthorizationRenderer.Render(
            command.Authorize,
            new ApplicationSet([_application]),
            $"Command '{command.Name}'",
            _diagnostics));
    }

    [Fact] void should_block_the_parsed_conjunction() => _error.ShouldBeOfExactType<AuthorizationCannotBeRendered>();
    [Fact] void should_report_the_stable_error_code() => _error.Message.ShouldContain("STAGE-AUTH-001:");
}
#endif
