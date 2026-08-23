// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Types;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application_with_authorization : an_application_with_authorization
{
    IReadOnlyList<string> _compilationErrors = null!;
    Type _register = null!;
    MethodInfo _all = null!;
    MethodInfo _mine = null!;
    MethodInfo _authenticatedOnly = null!;

    async Task Because()
    {
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _compilationErrors = RenderedOutput.Errors(_codeOutput.Files);
        var assembly = RenderedOutput.Load(_codeOutput.Files);
        _register = assembly.GetTypes().Single(type => type.Name == "RegisterInvoice");
        var summary = assembly.GetTypes().Single(type => type.Name == "InvoiceSummary");
        _all = summary.GetMethod("All")!;
        _mine = summary.GetMethod("Mine")!;
        _authenticatedOnly = summary.GetMethod("AuthenticatedOnly")!;
    }

    // Asserted as joined text rather than an empty collection so a failure names the compilation errors.
    [Fact] void should_render_output_that_compiles() =>
        string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);
    [Fact] void should_import_the_authorization_namespace_into_the_command_slice() =>
        SliceContent("Register").ShouldContain("using Cratis.Arc.Authorization;");
    [Fact] void should_guard_the_command_with_the_roles_it_authorizes() =>
        SliceContent("Register").ShouldContain("[Roles(\"Administrator\", \"Auditor\")]");
    [Fact] void should_state_that_a_command_without_authorization_is_anonymous() =>
        SliceContent("Archive").ShouldContain("[AllowAnonymous]");
    [Fact] void should_not_union_distinct_query_policies_on_the_read_model() =>
        SliceContent("Summary").ShouldNotContain("[Roles(\"Administrator\", \"Auditor\")]");
    [Fact] void should_guard_all_with_only_its_own_policy() =>
        SliceContent("Summary").ShouldContain("[Roles(\"Administrator\")]\n    public static IQueryable<InvoiceSummary> All(");
    [Fact] void should_guard_mine_with_only_its_own_policy() =>
        SliceContent("Summary").ShouldContain("[Roles(\"Auditor\")]\n    public static IQueryable<InvoiceSummary> Mine(");
    [Fact] void should_guard_authenticated_only_exactly() =>
        SliceContent("Summary").ShouldContain("[Authorize]\n    public static IQueryable<InvoiceSummary> AuthenticatedOnly(");
    [Fact] void should_not_invent_a_query_the_document_never_declared() =>
        SliceContent("Summary").ShouldNotContain("InvoiceSummaryById");

    // All three queries are plain declarations, so every one renders as a separately authorized method and none
    // is reported. Only a query stating narrowing it cannot get - a filter, or a performer - still is.
    [Fact] void should_not_report_a_query_it_now_renders() =>
        _error.ToString().ShouldNotContain("Slice 'Summary' declares 3 query declaration(s) with no rendered equivalent");
    [Fact] void should_mark_the_property_the_constraint_keeps_unique() =>
        SliceContent("Register").ShouldContain("[property: Unique(\"UniqueInvoiceNumber\")]");
    [Fact] void should_import_the_constraint_namespace_into_the_command_slice() =>
        SliceContent("Register").ShouldContain("using Cratis.Chronicle.Events.Constraints;");
    [Fact] void should_no_longer_report_the_constraint_as_unrendered() =>
        _error.ToString().ShouldNotContain("Slice 'Register' declares 1 constraint declaration(s)");
    [Fact] void should_report_the_screen_it_does_not_render() =>
        _error.ToString().ShouldContain("Slice 'Summary' declares 1 screen declaration(s) with no rendered equivalent");
    [Fact] void should_report_the_personas_it_does_not_render() =>
        _error.ToString().ShouldContain("1 persona declaration(s) are not rendered");
    [Fact] void should_report_the_authentication_providers_it_does_not_render() =>
        _error.ToString().ShouldContain("1 authentication provider(s) are not rendered");

    [Fact] void should_admit_an_administrator_to_the_generated_role_alternative() =>
        IsAuthorized(_register, Authenticated("Administrator")).ShouldBeTrue();
    [Fact] void should_admit_an_auditor_to_the_generated_role_alternative() =>
        IsAuthorized(_register, Authenticated("Auditor")).ShouldBeTrue();
    [Fact] void should_reject_an_unrelated_role_from_the_generated_role_alternative() =>
        IsAuthorized(_register, Authenticated("Support")).ShouldBeFalse();
    [Fact] void should_reject_an_authenticated_caller_with_no_role_from_the_generated_role_alternative() =>
        IsAuthorized(_register, Authenticated()).ShouldBeFalse();
    [Fact] void should_reject_an_unauthenticated_caller_from_the_generated_role_alternative() =>
        IsAuthorized(_register, Unauthenticated()).ShouldBeFalse();
    [Fact] void should_reject_a_missing_principal_from_the_generated_role_alternative() =>
        IsAuthorized(_register, null).ShouldBeFalse();

    [Fact] void should_admit_only_an_administrator_to_all() =>
        IsAuthorized(_all, Authenticated("Administrator")).ShouldBeTrue();
    [Fact] void should_reject_an_auditor_from_all() =>
        IsAuthorized(_all, Authenticated("Auditor")).ShouldBeFalse();
    [Fact] void should_admit_only_an_auditor_to_mine() =>
        IsAuthorized(_mine, Authenticated("Auditor")).ShouldBeTrue();
    [Fact] void should_reject_an_administrator_from_mine() =>
        IsAuthorized(_mine, Authenticated("Administrator")).ShouldBeFalse();
    [Fact] void should_admit_an_authenticated_caller_without_roles_to_authenticated_only() =>
        IsAuthorized(_authenticatedOnly, Authenticated()).ShouldBeTrue();
    [Fact] void should_reject_an_unauthenticated_caller_from_authenticated_only() =>
        IsAuthorized(_authenticatedOnly, Unauthenticated()).ShouldBeFalse();
    [Fact] void should_reject_a_missing_principal_from_authenticated_only() =>
        IsAuthorized(_authenticatedOnly, null).ShouldBeFalse();

    static ClaimsPrincipal Authenticated(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "StageSpecs"));

    static ClaimsPrincipal Unauthenticated() => new(new ClaimsIdentity());

    static bool IsAuthorized(Type type, ClaimsPrincipal? principal) => Evaluator(principal).IsAuthorized(type);

    static bool IsAuthorized(MethodInfo method, ClaimsPrincipal? principal) => Evaluator(principal).IsAuthorized(method);

    static AuthorizationEvaluator Evaluator(ClaimsPrincipal? principal) =>
        new(
            new CurrentPrincipal(principal),
            new KnownInstancesOf<IAnonymousEvaluator>(new AnonymousEvaluator()),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>(new AuthorizationAttributeEvaluator()));

    string SliceContent(string slice) =>
        _codeOutput.Files.Single(file => file.RelativePath.EndsWith(Path.Combine(slice, $"{slice}.cs"), StringComparison.Ordinal)).Content;

    sealed class CurrentPrincipal(ClaimsPrincipal? principal) : ICurrentPrincipalAccessor
    {
        public ClaimsPrincipal? Current => principal;
    }
}
