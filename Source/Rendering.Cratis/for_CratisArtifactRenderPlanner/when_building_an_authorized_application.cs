// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_building_an_authorized_application : a_generated_invoice_application
{
    protected override string InvoiceSource => when_rendering_portable_authorization.Source + "\n" +
        invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("module Billing", "module OpenBilling", StringComparison.Ordinal)
            .Replace("IssueInvoice", "CreatePublicInvoice", StringComparison.Ordinal)
            .Replace("InvoiceIssued", "PublicInvoiceIssued", StringComparison.Ordinal);

    async Task Because()
    {
        AddGeneratedSpecification("when_rejecting_a_foreign_invoice.cs", """
            #if DEBUG
            using System.Security.Claims;
            using Cratis.Arc.Authorization;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Arc.Http;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Specifications;
            using Invoices.Billing.Invoicing.Issue;
            using Invoices.Common;
            using Microsoft.Extensions.DependencyInjection;
            using NSubstitute;
            using Xunit;

            namespace Invoices;

            public class when_rejecting_a_foreign_invoice : Specification
            {
                bool _authorized;
                int _appended;

                async Task Because()
                {
                    using var scenario = new CommandScenario<IssueInvoice>();
                    var request = Substitute.For<IHttpRequestContextAccessor>();
                    scenario.Services.AddSingleton(request);
                    GeneratedPolicies.Registration.Register(scenario.Services);
                    var accessor = new CurrentPrincipalAccessor(request);
                    using var scope = accessor.BeginScope(new ClaimsPrincipal(new ClaimsIdentity(
                        [new(ClaimTypes.Role, "Staff"), new("owner", "victim"), new("region", "North")], "fixture")));
                    var result = await scenario.Execute(new IssueInvoice(new InvoiceId("invoice-one"), "North"));
                    _authorized = result.IsAuthorized;
                    _appended = scenario.AppendedEvents.Count;
                }

                [Fact] void should_be_denied_by_the_arc_pipeline() => _authorized.ShouldBeFalse();
                [Fact] void should_not_append_an_event() => _appended.ShouldEqual(0);
            }
            #endif
            """);
        await VerifyGeneratedApplication();
    }

    [Fact] void should_build_debug_without_warnings() => DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_generated_specs() => Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed", "Passed", "Passed", "Passed"]);
}
#endif
