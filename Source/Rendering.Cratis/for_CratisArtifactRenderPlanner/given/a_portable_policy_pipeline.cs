// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public abstract class a_portable_policy_pipeline : a_generated_invoice_application
{
    protected override string InvoiceSource => ModelSource;
    protected abstract string ModelSource { get; }
    protected abstract IEnumerable<(string Name, string Principal, bool Allowed)> Vectors { get; }
    protected virtual string IdentifierExpression => "new InvoiceId(key)";
    protected virtual string Key => "invoice-one";
    protected virtual bool IncludeQuery => true;

    protected static string WithGuestDenials(string source) => source.Replace(
        "    slice StateView Lookup",
        """
              specification DenyingGuestCommand
                given caller
                when IssueInvoice
                  invoiceId = "invoice-one"
                  description = "North"
                then denied
            slice StateView Lookup
        """,
        StringComparison.Ordinal) + "\n" + """
              specification DenyingGuestQuery
                given caller
                then query InvoiceById
                  arguments
                    invoiceId = "invoice-one"
                then denied
        """;

    protected async Task VerifyPipeline()
    {
        var query = IncludeQuery ? """
                    using var query = new QueryScenario<InvoiceSummary>();
                    Invoices.GeneratedPolicies.Registration.Register(query.Services);
                    query.Services.AddSingleton<ICurrentPrincipalAccessor>(accessor);
                    query.Given.ForEventSource(key).Events(new InvoiceIssued(id, "North"));
                    await AssertGuestDenied(typeof(InvoiceSummary).GetMethod(nameof(InvoiceSummary.InvoiceById))!,
                        new QueryContext(nameof(InvoiceSummary.InvoiceById), CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["invoiceId"] = id }));
                    var queryResult = await query.Perform(nameof(InvoiceSummary.InvoiceById), new QueryArguments { ["invoiceId"] = id });
                    Assert.False(queryResult.HasExceptions, string.Join("; ", queryResult.ExceptionMessages));
                    return (commandResult.IsAuthorized, command.AppendedEvents.Count, queryResult.IsAuthorized, queryResult.Data is InvoiceSummary);
            """ : "return (commandResult.IsAuthorized, command.AppendedEvents.Count, false, false);";
        var queryAllowed = IncludeQuery ? "true" : "false";
        var assertions = string.Join(Environment.NewLine, Vectors.Select(vector => $$"""
                [Fact]
                public async Task should_{{vector.Name}}()
                {
                    var result = await PolicyProbe.Execute({{vector.Principal}}, {{JsonSerializer.Serialize(Key)}});
                    Assert.Equal(({{(vector.Allowed ? $"true, 1, {queryAllowed}, {queryAllowed}" : "false, 0, false, false")}}), result);
                }
            """));
        AddGeneratedSpecification("PolicyProbe.cs", $$"""
            #if DEBUG
            using System.Reflection;
            using System.Security.Claims;
            using Cratis.Arc.Authorization;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Queries;
            using Cratis.Arc.Http;
            using Cratis.Arc.Queries;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Testing.Queries;
            using Cratis.Execution;
            using Invoices.Billing.Invoicing.Issue;
            {{(IncludeQuery ? "using Invoices.Billing.Invoicing.Lookup;" : string.Empty)}}
            using Invoices.Common;
            using Microsoft.Extensions.DependencyInjection;
            using Xunit;
            using static Invoices.Probes.PolicyProbe;

            namespace Invoices.Probes;

            public static class PolicyProbe
            {
                public static ClaimsPrincipal Guest() => new(new ClaimsIdentity());
                public static ClaimsPrincipal Authenticated(params Claim[] claims) => new(new ClaimsIdentity(claims, "fixture"));

                public static async Task<(bool CommandAllowed, int Appended, bool QueryAllowed, bool HasData)> Execute(ClaimsPrincipal principal, string key)
                {
                    var id = {{IdentifierExpression}};
                    var accessor = new CurrentPrincipalAccessor(new HttpRequestContextAccessor());
                    using var scope = accessor.BeginScope(principal);
                    using var command = new CommandScenario<IssueInvoice>();
                    Invoices.GeneratedPolicies.Registration.Register(command.Services);
                    Assert.All(command.Services.Where(service => service.ImplementationInstance is AuthorizationPolicyRegistration), service =>
                        Assert.False(((AuthorizationPolicyRegistration)service.ImplementationInstance!).EvaluatesAnonymous));
                    command.Services.AddSingleton<ICurrentPrincipalAccessor>(accessor);
                    await AssertGuestDenied(typeof(IssueInvoice),
                        new CommandContext(CorrelationId.New(), typeof(IssueInvoice), new IssueInvoice(id, "North"), [], CommandContextValues.Empty));
                    var commandResult = await command.Execute(new IssueInvoice(id, "North"));
                    Assert.False(commandResult.HasExceptions, string.Join("; ", commandResult.ExceptionMessages));
                    {{query}}
                }

                static async Task AssertGuestDenied(MemberInfo target, object resource)
                {
                    var name = target.GetCustomAttribute<AuthorizeAttribute>()!.Policy;
                    var type = typeof(IssueInvoice).Assembly.GetTypes().Single(candidate => candidate.Name == name);
                    var policy = (IAuthorizationPolicy)Activator.CreateInstance(type)!;
                    Assert.False(await policy.IsAuthorized(new(Guest(), target, resource), CancellationToken.None));
                }
            }

            public class when_exercising_policy_vectors
            {
                {{assertions}}
            }
            #endif
            """);
        await VerifyGeneratedApplication();
    }
}
#endif
