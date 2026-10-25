// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_opaque_policies.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Runs an opaque policy through the generated Arc command and query pipelines with a fixed clock, proving the body
/// sees Arc's receipt time as <c language="csharp">PolicyContext.Occurred</c>.
/// </summary>
/// <param name="fixture">The generated application fixture.</param>
public class when_executing_opaque_policies(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_and_release_without_warnings() => (fixture.DebugWarnings + fixture.ReleaseWarnings).ShouldBeEmpty();
    [Fact] void should_run_every_vector() => fixture.Results.Length.ShouldEqual(4);
    [Fact] void should_decide_by_the_receipt_time_and_caller() => fixture.Results.All(result => result.Outcome == "Passed").ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        const string Body = "return context.Identity.HasRole(\"Clerk\") && context.Occurred.Year == 2027 && context.Occurred.Month == 3 && context.Occurred.Day == 4;";

        protected override string InvoiceSource => opaque_policy_model.Source(Body);

        protected override ArtifactRenderPlan CreatePlan() => opaque_policy_model.Plan(InvoiceSource);

        async Task Because()
        {
            AddGeneratedSpecification("OpaquePolicyProbe.cs", """
                #if DEBUG
                using System.Security.Claims;
                using Cratis.Arc.Authorization;
                using Cratis.Arc.Chronicle.Testing.Commands;
                using Cratis.Arc.Chronicle.Testing.Queries;
                using Cratis.Arc.Http;
                using Cratis.Arc.Queries;
                using Cratis.Arc.Testing.Commands;
                using Cratis.Arc.Testing.Queries;
                using Invoices.Billing.Invoicing.Issue;
                using Invoices.Billing.Invoicing.Lookup;
                using Invoices.Common;
                using Microsoft.Extensions.DependencyInjection;
                using Xunit;

                namespace Invoices.Probes;

                public sealed class FixedReceipt(DateTimeOffset now) : TimeProvider
                {
                    public override DateTimeOffset GetUtcNow() => now;
                }

                public static class OpaquePolicyProbe
                {
                    public static readonly DateTimeOffset Ruled = new(2027, 3, 4, 10, 0, 0, TimeSpan.Zero);

                    public static ClaimsPrincipal Caller(params string[] roles) =>
                        new(new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "fixture"));

                    public static async Task<(bool CommandAllowed, int Appended, bool QueryAllowed, bool HasData)> Execute(ClaimsPrincipal principal, DateTimeOffset receipt)
                    {
                        var id = new InvoiceId("invoice-one");
                        var accessor = new CurrentPrincipalAccessor(new HttpRequestContextAccessor());
                        using var scope = accessor.BeginScope(principal);
                        using var command = new CommandScenario<IssueInvoice>();
                        Invoices.GeneratedPolicies.Registration.Register(command.Services);
                        command.Services.AddSingleton<ICurrentPrincipalAccessor>(accessor);
                        command.Services.AddSingleton<TimeProvider>(new FixedReceipt(receipt));
                        var commandResult = await command.Execute(new IssueInvoice(id, "North"));
                        Assert.False(commandResult.HasExceptions, string.Join("; ", commandResult.ExceptionMessages));

                        using var query = new QueryScenario<InvoiceSummary>();
                        Invoices.GeneratedPolicies.Registration.Register(query.Services);
                        query.Services.AddSingleton<ICurrentPrincipalAccessor>(accessor);
                        query.Services.AddSingleton<TimeProvider>(new FixedReceipt(receipt));
                        query.Given.ForEventSource((Cratis.Chronicle.Events.EventSourceId)id).Events(new InvoiceIssued(id, "North"));
                        var queryResult = await query.Perform(nameof(InvoiceSummary.InvoiceById), new QueryArguments { ["invoiceId"] = id });
                        Assert.False(queryResult.HasExceptions, string.Join("; ", queryResult.ExceptionMessages));
                        return (commandResult.IsAuthorized, command.AppendedEvents.Count, queryResult.IsAuthorized, queryResult.Data is InvoiceSummary);
                    }
                }

                public class when_exercising_opaque_policy_vectors
                {
                    [Fact]
                    public async Task should_allow_a_clerk_received_on_the_ruled_day() =>
                        Assert.Equal((true, 1, true, true), await OpaquePolicyProbe.Execute(OpaquePolicyProbe.Caller("Clerk"), OpaquePolicyProbe.Ruled));

                    [Fact]
                    public async Task should_deny_a_clerk_received_on_another_day() =>
                        Assert.Equal((false, 0, false, false), await OpaquePolicyProbe.Execute(OpaquePolicyProbe.Caller("Clerk"), OpaquePolicyProbe.Ruled.AddDays(1)));

                    [Fact]
                    public async Task should_deny_another_role_received_on_the_ruled_day() =>
                        Assert.Equal((false, 0, false, false), await OpaquePolicyProbe.Execute(OpaquePolicyProbe.Caller("Staff"), OpaquePolicyProbe.Ruled));

                    [Fact]
                    public async Task should_deny_a_guest() =>
                        Assert.Equal((false, 0, false, false), await OpaquePolicyProbe.Execute(new ClaimsPrincipal(new ClaimsIdentity()), OpaquePolicyProbe.Ruled));
                }
                #endif
                """);
            await VerifyGeneratedApplication();
        }
    }
}
#endif
