// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CodeProvidedReadModels;

/// <summary>The executable probe compiled inside the generated application, not a source-string assertion.</summary>
internal static class InvoicingExecutionProof
{
    internal const string Source = """
        using System.Security.Claims;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Availability;
        using Cratis.Arc.MongoDB;
        using Cratis.Choreography.Availability.Audiences;
        using Cratis.Chronicle;
        using Cratis.Specifications;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;
        using NSubstitute;
        using Xunit;
        using IssueInvoice = Invoicing.Billing.Invoices.IssuingInvoice.IssueInvoice;
        using Invoice = Invoicing.Billing.Invoices.ListingInvoices.Invoice;
        namespace Invoicing;
        public class when_deciding_invoice_availability : Specification
        {
            [Fact]
            async Task should_decide_both_outcomes_from_the_actual_file_and_inline_values()
            {
                using var services = Services(out var principal);
                using var identity = principal.BeginScope(new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, "alice"), new Claim("scope", "invoices"), new Claim(ClaimTypes.Role, "editor") }, "proof")));
                var reads = services.GetRequiredService<ICurrentPrincipalAccessor>();
                reads.ClearReceivedCalls();
                var gate = (IAvailabilityExecutionGate)services.GetRequiredService<IAvailabilityGate>();
                var source = (Cratis.Arc.Contexts.IExecutionContextValueSource)services.GetRequiredService<Cratis.Arc.Contexts.IContextValueSource>();
                var occurred = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);
                var allowed = new AvailabilityInvocation(typeof(IssueInvoice), new IssueInvoice(InvoiceId: Guid.Empty, Amount: 42000L), occurred);
                var refused = new AvailabilityInvocation(typeof(IssueInvoice), new IssueInvoice(InvoiceId: Guid.Empty, Amount: 50001L), occurred.AddMinutes(1));
                gate.IsGated(typeof(IssueInvoice)).ShouldBeTrue();
                (await gate.Decide(allowed, services, default)).Verdict.ShouldEqual(AvailabilityVerdict.Available);
                _ = reads.Received(2).Current;
                (await source.ReadForExecution(allowed, "creditCeiling", default)).Value.ShouldEqual(50000L);
                (await source.ReadForExecution(allowed, "reminderDays", default)).Value.ShouldEqual(14L);
                _ = reads.Received(2).Current;
                (await gate.Decide(refused, services, default)).Verdict.ShouldEqual(AvailabilityVerdict.Unavailable);
                (await source.ReadForExecution(refused, "creditCeiling", default)).Value.ShouldEqual(50000L);
                (await source.ReadForExecution(refused, "reminderDays", default)).Value.ShouldEqual(14L);
                _ = reads.Received(4).Current;
                (await source.ReadForExecution(allowed, "boundAmount", default)).Value.ShouldEqual(42000L);
                (await source.ReadForExecution(refused, "boundAmount", default)).Value.ShouldEqual(50001L);
                (await source.ReadForExecution(allowed, "observedAt", default)).Value.ShouldEqual(occurred.ToString("O"));
                (await source.ReadForExecution(refused, "observedAt", default)).Value.ShouldEqual(occurred.AddMinutes(1).ToString("O"));
                (await source.ReadForExecution(allowed, "argumentType", default)).Value.ShouldEqual("IssueInvoice");
                (await source.ReadForExecution(allowed, "tenantName", default)).Value.ShouldEqual("named-tenant");
                (await source.ReadForExecution(allowed, "caller", default)).Value.ShouldEqual("alice:invoices");
                var query = new AvailabilityInvocation(typeof(Invoice), new Dictionary<string, object?> { ["Amount"] = 14L }, occurred.AddMinutes(2));
                (await source.ReadForExecution(query, "boundAmount", default)).Value.ShouldEqual(14L);
                (await source.ReadForExecution(query, "observedAt", default)).Value.ShouldEqual(occurred.AddMinutes(2).ToString("O"));
            }

            [Fact]
            async Task should_preserve_optional_and_required_absence_and_read_model_results()
            {
                using var services = Services(out _);
                var source = (Cratis.Arc.Contexts.IExecutionContextValueSource)services.GetRequiredService<Cratis.Arc.Contexts.IContextValueSource>();
                var invocation = new AvailabilityInvocation(typeof(IssueInvoice), new IssueInvoice(InvoiceId: Guid.Empty, Amount: 1L), new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
                var preview = await source.ReadForExecution(invocation, "preview", default);
                preview.IsPresent.ShouldBeTrue();
                ((Invoice)preview.Value!).Amount.ShouldEqual(50000L);
                (await source.ReadForExecution(invocation, "missingOptional", default)).IsPresent.ShouldBeFalse();
                await Assert.ThrowsAsync<Cratis.Arc.Contexts.RequiredContextValueAbsentException>(async () =>
                    await source.ReadForExecution(invocation, "missingRequired", default));
            }

            static ServiceProvider Services(out CurrentPrincipalAccessor principal)
            {
                var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
                builder.Environment.EnvironmentName = "Testing";
                global::Invoicing.GeneratedAvailability.Registration.Register(builder);
                builder.AddCratis(
                    configureArcBuilder: arc => arc.WithMongoDB(),
                    configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy());
                global::Invoicing.GeneratedPolicies.Registration.Register(builder.Services);
                global::Invoicing.GeneratedProvides.Registration.Register(builder.Services);
                var currentPrincipal = new CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor());
                principal = currentPrincipal;
                var reads = Substitute.For<ICurrentPrincipalAccessor>();
                reads.Current.Returns(_ => currentPrincipal.Current);
                builder.Services.AddSingleton(reads);
                var subject = Substitute.For<IAvailabilitySubjectProvider>();
                subject.GetSubject().Returns(new AudienceSubject("named-tenant", "alice"));
                builder.Services.AddSingleton(subject);
                builder.Services.AddSingleton(Substitute.For<IAudienceResolver>());
                return builder.Services.BuildServiceProvider();
            }
        }
        """;
}
#endif
