// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_enumeration_storage : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Text.Json;
        using System.Text.Json.Nodes;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Json;
        using Cratis.Chronicle.Schemas;
        using Cratis.Chronicle.Testing;
        using Invoices.Billing.Invoicing.Issue;
        using Invoices.Common;
        using Xunit;

        namespace Invoices.EnumerationProbe;

        public class when_storing_a_generated_enumeration
        {
            static readonly string[] MemberNames = ["Draft", "Issued"];

            [Fact]
            public async Task should_store_the_member_number_with_an_integer_enumeration_schema()
            {
                using var scenario = new CommandScenario<IssueInvoice>();
                var result = await scenario.Execute(new IssueInvoice(new InvoiceId("invoice-one"), Status.Issued), cancellationToken: TestContext.Current.CancellationToken);
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var appended = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<InvoiceIssued>(appended.Content);
                var json = JsonSerializer.Serialize(payload, Cratis.Json.Globals.JsonSerializerOptions);
                Assert.Equal("{\"status\":1}", json);

                var schema = Defaults.Instance.JsonSchemaGenerator.Generate(typeof(InvoiceIssued));
                var property = schema.Properties["status"];
                var enumeration = property.Reference ?? property;
                Assert.Equal(JsonObjectType.Integer, enumeration.Type);
                Assert.Equal(MemberNames, enumeration.EnumerationNames);
                Assert.Contains("\"x-enumNames\"", enumeration.ToJson());

                // The kernel uses this converter on append, including when a writer sends a name.
                var converter = new ExpandoObjectConverter(new TypeFormats());
                var numeric = converter.ToExpandoObject(JsonNode.Parse(json)!.AsObject(), schema);
                var named = converter.ToExpandoObject(JsonNode.Parse("{\"status\":\"Issued\"}")!.AsObject(), schema);
                Assert.Equal("{\"status\":1}", JsonSerializer.Serialize(numeric));
                Assert.Equal("{\"status\":1}", JsonSerializer.Serialize(named));
            }
        }
        #endif
        """;

    string _build = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(an_enumeration_model.Compile());

    async Task Because()
    {
        AddGeneratedSpecification("when_storing_a_generated_enumeration.cs", Probe);
        _build = await Run("enumeration-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _tests = await Run("enumeration-tests.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact] void should_build_without_warnings() => BuildWarnings(_build).ShouldEqual(string.Empty);
    [Fact] void should_confirm_the_generated_applications_numeric_storage_contract() => _tests.ShouldHavePassedSpecifications();
}
#endif
