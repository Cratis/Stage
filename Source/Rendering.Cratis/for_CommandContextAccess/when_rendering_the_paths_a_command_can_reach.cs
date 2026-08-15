// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CommandContextAccess;

public class when_rendering_the_paths_a_command_can_reach : Specification
{
    readonly List<string> _diagnostics = [];
    CommandContextAccess _access = null!;
    string _occurred = null!;
    string _tenant = null!;
    string _identityId = null!;
    string _identityName = null!;
    string _isAuthenticated = null!;
    string _claim = null!;
    string _roles = null!;
    string _causedBySubject = null!;
    string _causationType = null!;
    string _commandProperty = null!;

    void Establish() => _access = new CommandContextAccess("Command 'RegisterInvoice'", _diagnostics);

    void Because()
    {
        _occurred = _access.Render(Context("occurred"));
        _tenant = _access.Render(Context("tenant"));
        _identityId = _access.Render(Context("identity.id"));
        _identityName = _access.Render(Context("identity.name"));
        _isAuthenticated = _access.Render(Context("identity.isAuthenticated"));
        _claim = _access.Render(Context("identity.claims.department"));
        _roles = _access.Render(Context("identity.roles"));
        _causedBySubject = _access.Render(new CausedByExpressionSyntax("subject", SourceLocation.Start));
        _causationType = _access.Render(Context("causation.type"));
        _commandProperty = _access.Render(Context("command.invoiceNumber"));
    }

    [Fact] void should_render_occurred_as_the_time_the_handler_runs() => _occurred.ShouldEqual("DateTimeOffset.UtcNow");
    [Fact] void should_render_the_tenant_as_its_value() => _tenant.ShouldEqual("tenants.Current.Value");
    [Fact] void should_render_the_identity_id_as_the_subject() => _identityId.ShouldEqual("identities.GetCurrent().Subject");
    [Fact] void should_render_the_identity_name() => _identityName.ShouldEqual("identities.GetCurrent().Name");
    [Fact] void should_render_whether_the_caller_is_authenticated() =>
        _isAuthenticated.ShouldEqual("principals.Current?.Identity?.IsAuthenticated == true");
    [Fact] void should_render_a_claim_by_name() =>
        _claim.ShouldEqual("principals.Current?.FindFirst(\"department\")?.Value ?? string.Empty");
    [Fact] void should_render_the_roles_the_caller_holds() =>
        _roles.ShouldEqual("(principals.Current?.FindAll(ClaimTypes.Role) ?? []).Select(claim => claim.Value)");

    // The language says Identity.Id and CausedBy.Subject are the same value seen from the decision and the audit
    // side, so they resolve to one expression rather than to two collaborators that could disagree.
    [Fact] void should_render_the_causing_subject_as_the_same_value_as_the_identity_id() => _causedBySubject.ShouldEqual(_identityId);

    [Fact] void should_render_the_causation_type_as_its_value() =>
        _causationType.ShouldEqual("causations.GetCurrentChain()[^1].Type.Value");
    [Fact] void should_render_a_command_property_as_the_command_s_own() => _commandProperty.ShouldEqual("InvoiceNumber");
    [Fact] void should_report_nothing_as_unreachable() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_ask_for_each_collaborator_once() => _access.Collaborators.Count.ShouldEqual(4);
    [Fact] void should_name_the_identity_provider_in_full_because_the_short_name_is_ambiguous() =>
        _access.Collaborators.ShouldContain(HandlerCollaborator.Identities);
    [Fact] void should_import_what_the_rendered_expressions_need() =>
        _access.Namespaces.ShouldContainOnly(["Cratis.Arc.Authorization", "Cratis.Arc.Tenancy", "Cratis.Chronicle.Auditing", "System.Security.Claims"]);

    static ContextExpressionSyntax Context(string path) => new(path, SourceLocation.Start);
}
