// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Types;
using Xunit;
using UnsupportedAuthorization = Cratis.Stage.Rendering.Cratis.Authorization.AuthorizationCannotBeRendered;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application_with_unsupported_authorization : an_application_with_authorization
{
    bool _bareAuthorizeAdmitsAuthenticatedPrincipal;
    Exception _exception = null!;

    void Establish() => RequireAllPoliciesForRegister("Administrator", "Auditor");

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

        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "StageSpecs"));
        var evaluator = new AuthorizationEvaluator(
            new CurrentPrincipal(principal),
            new KnownInstancesOf<IAnonymousEvaluator>(new AnonymousEvaluator()),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>(new AuthorizationAttributeEvaluator()));
        _bareAuthorizeAdmitsAuthenticatedPrincipal = evaluator.IsAuthorized(typeof(BareAuthorizeArtifact));
    }

    [Fact] void should_prove_that_a_bare_authorize_fallback_would_be_permissive() =>
        _bareAuthorizeAdmitsAuthenticatedPrincipal.ShouldBeTrue();
    [Fact] void should_fail_the_render_operation() => _exception.ShouldBeOfExactType<RenderingFailed>();
    [Fact] void should_preserve_the_authorization_failure() =>
        ((RenderingFailed)_exception).Failures.ShouldContain(failure => failure is UnsupportedAuthorization);
    [Fact] void should_not_report_unqualified_completion() => _output.ToString().ShouldNotContain("Rendering complete.");
    [Fact] void should_write_the_advisory_failure_marker() => _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
    [Fact] void should_not_emit_the_unsupported_command_artifact() =>
        _codeOutput.Files.ShouldNotContain(file => file.Content.Contains("record RegisterInvoice", StringComparison.Ordinal));
    [Fact] void should_continue_rendering_other_artifacts_without_making_the_blocked_one_runnable() =>
        _codeOutput.Files.ShouldContain(file => file.Content.Contains("record ArchiveInvoice", StringComparison.Ordinal));
    [Fact] void should_report_the_blocking_authorization_error() =>
        _error.ToString().ShouldContain("STAGE-AUTH-001: Command 'RegisterInvoice' declares a conjunction");
    [Fact] void should_not_claim_that_a_protected_fallback_was_emitted() =>
        _error.ToString().ShouldNotContain("protected fallback");
    [Fact] void should_warn_that_stale_blocked_artifacts_may_remain() =>
        _error.ToString().ShouldContain("files from earlier runs may remain, including artifacts blocked by this run");

    [Authorize]
    sealed class BareAuthorizeArtifact;

    sealed class CurrentPrincipal(ClaimsPrincipal principal) : ICurrentPrincipalAccessor
    {
        public ClaimsPrincipal? Current => principal;
    }
}
#endif
