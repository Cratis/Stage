// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_portable_authorization : Specification
{
    internal const string Source = """
        concept InvoiceId : String
        policy Access
          require authenticated and (role "Staff" or claim "department" matches "Sales")
        policy Owns
          require claim "owner" matches subject and claim "region" matches description
        policy OwnQuery
          require claim "owner" matches subject
        module Billing
          authorize Access
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                authorize Owns
                invoiceId InvoiceId identifier
                description String
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
                  description = description
              event InvoiceIssued
                invoiceId InvoiceId
                description String
            slice StateView Lookup
              readmodel InvoiceSummary
                invoiceId InvoiceId
                description String
              query InvoiceById => InvoiceSummary?
                by invoiceId InvoiceId
                authorize OwnQuery
              projection InvoiceSummaryProjection => InvoiceSummary
                from InvoiceIssued key invoiceId
                  invoiceId = invoiceId
                  description = description
        """;

    ArtifactRenderPlan _plan = null!;
    Assembly _assembly = null!;
    Type _command = null!;
    Type _query = null!;
    IAuthorizationPolicy _commandPolicy = null!;
    IAuthorizationPolicy _queryPolicy = null!;
    object _commandResource = null!;
    QueryContext _queryResource = null!;

    void Because()
    {
        var model = invoice_model.Compile(Source);
        _plan = invoice_model.Plan(model);
        if (!_plan.Success) return;
        var sources = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        _assembly = RenderedOutput.Load(sources);
        var command = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Commands.Length == 1).Commands.Single();
        var query = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Queries.Length == 1).Queries.Single();
        _commandPolicy = (IAuthorizationPolicy)Activator.CreateInstance(_assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(command.Id)))!;
        _queryPolicy = (IAuthorizationPolicy)Activator.CreateInstance(_assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(query.Id)))!;
        _command = _assembly.GetTypes().Single(type => type.Name == "IssueInvoice");
        _query = _assembly.GetTypes().Single(type => type.Name == "InvoiceSummary");
        var invoiceId = Activator.CreateInstance(_assembly.GetTypes().Single(type => type.Name == "InvoiceId"), "invoice-one")!;
        var instance = Activator.CreateInstance(_command, invoiceId, "North")!;
        _commandResource = new CommandContext(CorrelationId.New(), _command, instance, [], CommandContextValues.Empty);
        _queryResource = new QueryContext("InvoiceById", CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["invoiceId"] = invoiceId });
    }

    [Fact] void should_plan_both_protected_operations() => _plan.Success.ShouldBeTrue();
    [Fact] void should_register_both_named_policies()
    {
        var services = new ServiceCollection();
        _assembly.GetType("Invoices.GeneratedPolicies.Registration")!.GetMethod("Register")!.Invoke(null, [services]);
        services.Count(service => service.ServiceType == typeof(AuthorizationPolicyRegistration)).ShouldEqual(2);
    }

    [Fact] void should_mark_the_command_with_its_named_policy() => _command.GetCustomAttributes<AuthorizeAttribute>().Single().Policy.ShouldNotBeNull();
    [Fact] void should_mark_the_query_with_its_named_policy() => _query.GetMethod("InvoiceById")!.GetCustomAttributes<AuthorizeAttribute>().Single().Policy.ShouldNotBeNull();
    [Fact] void should_deny_anonymous_callers() => Allows(_commandPolicy, _command, _commandResource, Principal(false, "Staff", "invoice-one", "North")).ShouldBeFalse();
    [Fact] void should_allow_matching_role_claim_and_subject() => Allows(_commandPolicy, _command, _commandResource, Principal(true, "Staff", "invoice-one", "North")).ShouldBeTrue();
    [Fact] void should_allow_the_alternative_department_claim() => Allows(_commandPolicy, _command, _commandResource, Principal(true, null, "invoice-one", "North", "Sales")).ShouldBeTrue();
    [Fact] void should_allow_a_repeated_claim_when_one_value_matches() => Allows(_commandPolicy, _command, _commandResource, new ClaimsPrincipal(new ClaimsIdentity(
        [new("department", "Other"), new("DEPARTMENT", "Sales"), new("owner", "invoice-one"), new("region", "North")], "fixture"))).ShouldBeTrue();
    [Fact] void should_deny_wrong_casing_of_the_role() => Allows(_commandPolicy, _command, _commandResource, Principal(true, "staff", "invoice-one", "North")).ShouldBeFalse();
    [Fact] void should_deny_wrong_casing_of_the_claim_value() => Allows(_commandPolicy, _command, _commandResource, Principal(true, "Staff", "invoice-one", "north")).ShouldBeFalse();
    [Fact] void should_deny_wrong_subject() => Allows(_commandPolicy, _command, _commandResource, Principal(true, "Staff", "invoice-two", "North")).ShouldBeFalse();
    [Fact] void should_deny_an_anonymous_query() => Allows(_queryPolicy, _query.GetMethod("InvoiceById")!, _queryResource, Principal(false, "Staff", "invoice-one", null)).ShouldBeFalse();
    [Fact] void should_allow_an_authorized_query() => Allows(_queryPolicy, _query.GetMethod("InvoiceById")!, _queryResource, Principal(true, "Staff", "invoice-one", null)).ShouldBeTrue();
    [Fact] void should_deny_a_query_for_another_subject() => Allows(_queryPolicy, _query.GetMethod("InvoiceById")!, _queryResource, Principal(true, "Staff", "invoice-two", null)).ShouldBeFalse();
    [Fact] void should_deny_a_query_without_its_key() => Allows(
        _queryPolicy,
        _query.GetMethod("InvoiceById")!,
        _queryResource with { Arguments = QueryArguments.Empty },
        Principal(true, "Staff", "invoice-one", null)).ShouldBeFalse();

    static bool Allows(IAuthorizationPolicy policy, MemberInfo target, object resource, ClaimsPrincipal principal) =>
        policy.IsAuthorized(new(principal, target, resource), CancellationToken.None).AsTask().GetAwaiter().GetResult();

    static ClaimsPrincipal Principal(bool authenticated, string? role, string? owner, string? region, string? department = null)
    {
        var claims = new List<Claim>();
        if (role is not null) claims.Add(new(ClaimTypes.Role, role));
        if (owner is not null) claims.Add(new("OWNER", owner));
        if (region is not null) claims.Add(new("region", region));
        if (department is not null) claims.Add(new("department", department));
        return new(new ClaimsIdentity(claims, authenticated ? "fixture" : null));
    }
}
