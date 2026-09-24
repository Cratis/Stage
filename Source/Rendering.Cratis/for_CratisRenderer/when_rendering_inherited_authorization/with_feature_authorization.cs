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

public class with_feature_authorization : an_application_with_authorization
{
    RenderingFailed _failure = null!;

    void Establish()
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        _application = _application with { Modules = [module with { Features = [feature with { Authorize = new(new PolicyReferenceSyntax("Authenticated", SourceLocation.Start), SourceLocation.Start) }] }] };
    }

    async Task Because()
    {
        _failure = (RenderingFailed)await Catch.Exception(() => _renderer.Render([_application], _targetDirectory, _output, _error));
    }

    [Fact] void should_fail_closed_for_the_command() => _failure.Failures.OfType<AuthorizationCannotBeRendered>().Any(failure => failure.Message.Contains("Command", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_fail_closed_for_the_query() => _failure.Failures.OfType<AuthorizationCannotBeRendered>().Any(failure => failure.Message.Contains("Query", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_emit_protected_artifacts() => _codeOutput.Files.Any(file => file.Content.Contains("record RegisterInvoice", StringComparison.Ordinal) || file.Content.Contains("static IQueryable<InvoiceSummary> All(", StringComparison.Ordinal)).ShouldBeFalse();
}
#endif
