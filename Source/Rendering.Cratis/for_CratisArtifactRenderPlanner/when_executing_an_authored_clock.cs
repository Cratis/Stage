// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_an_authored_clock : a_generated_application
{
    const string Probe = """
        #if DEBUG
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using Invoices.Billing.Invoicing.Issue;
        using Invoices.Common;
        using Xunit;

        namespace Invoices.ClockProbe;

        public class when_using_the_authored_clock
        {
            [Fact]
            public async Task should_preserve_payload_and_stored_occurrence()
            {
                using var scenario = new CommandScenario<IssueInvoice>();
                var instant = new DateTimeOffset(2027, 3, 4, 10, 0, 0, 123, TimeSpan.Zero);
                scenario.Clock(instant);
                using var principalScope1 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                var result = await scenario.Execute(new IssueInvoice(new InvoiceId("invoice-one")));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var stored = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                Assert.Equal(instant, Assert.IsType<InvoiceIssued>(stored.Content).IssuedAt);
                Assert.Equal(instant, stored.Context.Occurred);
            }
        }
        #endif
        """;

    string _debug = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_clock_model.Source));

    async Task Because()
    {
        AddGeneratedSpecification("clock_probe.cs", Probe);
        _debug = await Run("clock-debug.log", "build", "-c", "Debug", "-warnaserror");
        _tests = await Run("clock-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_execute_the_fixed_occurrence_and_metadata_assertions()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        _tests.ShouldContain("Passed!");
    }
}
#endif
