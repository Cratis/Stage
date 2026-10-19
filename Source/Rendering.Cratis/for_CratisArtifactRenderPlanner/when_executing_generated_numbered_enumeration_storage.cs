// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_numbered_enumeration_storage : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Text.Json;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using Invoices.Billing.Invoicing.Issue;
        using Invoices.Common;
        using Xunit;

        namespace Invoices.EnumerationProbe;

        public class when_storing_numbered_enumerations
        {
            [Theory]
            [InlineData(Status.Draft, -1)]
            [InlineData(Status.Issued, 90)]
            public async Task should_store_the_model_owned_number(Status status, int number)
            {
                using var scenario = new CommandScenario<IssueInvoice>();
                using var principalScope1 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                var result = await scenario.Execute(new IssueInvoice(new InvoiceId("invoice-one"), status));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var appended = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<InvoiceIssued>(appended.Content);
                var json = JsonSerializer.Serialize(payload, Cratis.Json.Globals.JsonSerializerOptions);
                Assert.Equal(number, JsonDocument.Parse(json).RootElement.GetProperty("status").GetInt32());
            }
        }
        #endif
        """;

    string _build = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(an_enumeration_model.Compile(
        an_enumeration_model.Source.Replace("  draft\n  issued", "  draft = -1\n  issued = 90", StringComparison.Ordinal)));

    async Task Because()
    {
        AddGeneratedSpecification("when_storing_numbered_enumerations.cs", Probe);
        _build = await Run("numbered-enumeration-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _tests = await Run("numbered-enumeration-tests.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact] void should_build_without_warnings() => BuildWarnings(_build).ShouldEqual(string.Empty);
    [Fact] void should_store_sparse_and_negative_numbers() => _tests.ShouldContain("Passed!");
}
#endif
