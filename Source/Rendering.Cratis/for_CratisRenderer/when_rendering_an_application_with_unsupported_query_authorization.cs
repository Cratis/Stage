// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;
using UnsupportedAuthorization = Cratis.Stage.Rendering.Cratis.Authorization.AuthorizationCannotBeRendered;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application_with_unsupported_query_authorization : an_application_with_authorization
{
    const string StaleContent = "stale previously rendered query artifact";
    Exception _exception = null!;

    void Establish()
    {
        RequireAllPoliciesForQuery("All", "Administrator", "Auditor");
        _codeOutput.Write(
            new RenderedFile(Path.Combine("Billing", "Invoicing", "Summary", "Summary.cs"), StaleContent),
            _targetDirectory,
            _output).GetAwaiter().GetResult();
    }

    async Task Because()
    {
        try
        {
            await _renderer.Render([_application], _targetDirectory, _output, _error);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_fail_the_render_operation() => _exception.ShouldBeOfExactType<RenderingFailed>();
    [Fact] void should_preserve_the_query_authorization_failure() =>
        ((RenderingFailed)_exception).Failures.ShouldContain(failure => failure is UnsupportedAuthorization);
    [Fact] void should_report_the_specific_query_method() =>
        _error.ToString().ShouldContain("STAGE-AUTH-001: Query 'All' declares a conjunction");
    [Fact] void should_not_emit_a_new_state_view_artifact() =>
        _codeOutput.Files.ShouldNotContain(file => file.Content.Contains("public record InvoiceSummary", StringComparison.Ordinal));
    [Fact] void should_continue_rendering_independent_artifacts() =>
        _codeOutput.Files.ShouldContain(file => file.Content.Contains("record RegisterInvoice", StringComparison.Ordinal));
    [Fact] void should_leave_the_prior_artifact_physically_present() =>
        _codeOutput.Files.ShouldContain(file => file.Content == StaleContent);
    [Fact] void should_mark_the_target_unsafe_without_claiming_to_remove_the_stale_artifact() =>
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
}
#endif
