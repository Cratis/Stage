// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer.when_rendering_inherited_authorization;

// A read model with no declared query still gets synthesized all/by-id queries, so it inherits the module's
// authorization as much as a declared query does.
public class with_module_authorization_and_a_projection_only_slice : an_application_with_authorization
{
    RenderingFailed _failure = null!;

    void Establish()
    {
        var module = _application.Modules.Single();
        _application = _application with
        {
            Modules =
            [
                module with
                {
                    Authorize = new(new PolicyReferenceSyntax("Authenticated", SourceLocation.Start), SourceLocation.Start),
                    Features = [.. module.Features.Select(feature => feature with
                    {
                        Slices = [.. feature.Slices.Select(slice => slice.Queries.Any() ? slice with { Queries = [] } : slice)]
                    })]
                }
            ]
        };
    }

    async Task Because() => _failure = (RenderingFailed)await Catch.Exception(() => _renderer.Render([_application], _targetDirectory, _output, _error));

    [Fact] void should_fail_closed_for_the_synthesized_queries() => _failure.Failures.OfType<AuthorizationCannotBeRendered>().Any(failure => failure.Message.Contains("Read model queries", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_emit_an_anonymous_read_model() => _codeOutput.Files.Any(file => file.Content.Contains("AllowAnonymous", StringComparison.Ordinal)).ShouldBeFalse();
}
#endif
