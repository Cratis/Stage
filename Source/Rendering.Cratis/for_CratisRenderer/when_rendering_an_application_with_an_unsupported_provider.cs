// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application_with_an_unsupported_provider : an_application_with_authorization
{
    async Task Because()
    {
        _application = _application with
        {
            Authentication = new AuthenticationSyntax(
                [
                    new AuthenticationProviderSyntax("EntraId", null, SourceLocation.Start),
                    new AuthenticationProviderSyntax("Okta", "Workforce", SourceLocation.Start)
                ],
                SourceLocation.Start)
        };
        await _renderer.Render([_application], _targetDirectory, _output, _error);
    }

    [Fact] void should_report_the_provider_authproxy_does_not_know() => _error.ToString().ShouldContain("Authentication provider 'Workforce' (kind 'Okta') is not one AuthProxy can sign people in with");
    [Fact] void should_not_report_the_provider_it_knows() => _error.ToString().ShouldNotContain("Authentication provider 'EntraId'");
    [Fact] void should_render_only_the_provider_it_knows() => _codeOutput.Files.Single(file => file.RelativePath == ".runtime/authproxy.application.json").Content.ShouldNotContain("Okta");
}
