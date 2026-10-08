// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_command_receipt_time : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Security.Claims;
        using Cratis.Arc;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Http;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using Invoices.Billing.Invoicing.Issue;
        using Invoices.Common;
        using Microsoft.Extensions.DependencyInjection;
        using Xunit;

        namespace Invoices.ReceiptProbe;

        public class when_using_the_dispatch_receipt
        {
            static readonly DateTimeOffset Receipt = new(2027, 3, 4, 10, 0, 0, 123, TimeSpan.Zero);

            [Fact]
            public async Task should_append_the_same_occurrence_the_opaque_policy_authorized()
            {
                using var scenario = new CommandScenario<IssueInvoice>();
                var clock = new FixedClock();
                scenario.Services.AddSingleton<TimeProvider>(clock);
                // CommandScenario's lazy Arc initialization replaces custom principal accessor registrations.
                using var caller = new CurrentPrincipalAccessor(new HttpRequestContextAccessor())
                    .BeginScope(new ClaimsPrincipal(new ClaimsIdentity([], "fixture")));
                GeneratedPolicies.Registration.Register(scenario.Services);
                var result = await scenario.Execute(new IssueInvoice(new InvoiceId("invoice-one"), "North"));
                Assert.True(result.IsSuccess, $"Authorized={result.IsAuthorized}; clock reads={clock.Calls}; authorization={result.AuthorizationFailureReason}; validation={System.Text.Json.JsonSerializer.Serialize(result.ValidationResults)}; exceptions={string.Join("; ", result.ExceptionMessages)}");
                Assert.Single(scenario.AppendedEvents);
                var stored = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<InvoiceIssued>(stored.Content);
                // The generated opaque policy admits only this exact Occurred.Ticks value.
                Assert.Equal(Receipt, payload.IssuedAt);
                Assert.Equal(Receipt, stored.Context.Occurred);
                Assert.Equal(1, clock.Calls);
            }

            [Fact]
            public void should_fail_closed_without_a_dispatch_receipt()
            {
                var command = new IssueInvoice(new InvoiceId("invoice-one"), "North");
                var error = Assert.ThrowsAny<Exception>(() => command.Handle(new OperationContextAccessor()));
                Assert.Equal("CommandReceiptTimeUnavailable", error.GetType().Name);
            }

            [Fact]
            public void should_fail_closed_with_an_unset_dispatch_receipt()
            {
                var command = new IssueInvoice(new InvoiceId("invoice-one"), "North");
                var error = Assert.ThrowsAny<Exception>(() => command.Handle(new ReceiptAccessor(default)));
                Assert.Equal("CommandReceiptTimeUnavailable", error.GetType().Name);
            }

            [Fact]
            public void should_truncate_payload_and_append_context_together()
            {
                var command = new IssueInvoice(new InvoiceId("invoice-one"), "North");
                var produced = command.Handle(new ReceiptAccessor(Receipt.AddTicks(4567)));
                Assert.Equal(Receipt, Assert.IsType<InvoiceIssued>(produced.Event).IssuedAt);
                Assert.Equal(Receipt, produced.Occurred);
            }

            sealed class FixedClock : TimeProvider
            {
                public int Calls { get; private set; }

                public override DateTimeOffset GetUtcNow()
                {
                    Calls++;
                    return Receipt;
                }
            }

            sealed class ReceiptAccessor(DateTimeOffset receivedAt) : IOperationContextAccessor
            {
                public DateTimeOffset? ReceivedAt => receivedAt;
            }
        }
        #endif
        """;

    string _debug = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var receipt = new DateTimeOffset(2027, 3, 4, 10, 0, 0, 123, TimeSpan.Zero);
        var source = opaque_policy_model.Source($"return context.Identity.IsAuthenticated && context.Occurred.Ticks == {receipt.Ticks}L;")
            .Replace("description = description\n", "description = description\n          issuedAt = $context.occurred\n", StringComparison.Ordinal)
            .Replace("event InvoiceIssued\n", "event InvoiceIssued\n        issuedAt DateTime\n", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(source);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        return plan;
    }

    async Task Because()
    {
        AddGeneratedSpecification("when_using_the_dispatch_receipt.cs", Probe);
        _debug = await Run("receipt-debug.log", "build", "-c", "Debug", "-warnaserror");
        _tests = await Run("receipt-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_execute_the_receipt_time_contract()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        _tests.ShouldContain("Passed!");
        ReadGeneratedFile("Billing/Invoicing/Issue/Issue.cs").ShouldNotContain("UtcNow");
    }
}
#endif
