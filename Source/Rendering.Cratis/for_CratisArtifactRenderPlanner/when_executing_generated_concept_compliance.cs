// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_concept_compliance : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Text.Json;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Testing.ReadModels;
        using Invoices.Common;
        using Invoices.Invoices.Lifecycle.Lookup;
        using Invoices.Invoices.Lifecycle.Record;
        using Xunit;

        namespace Invoices.ComplianceProbe;

        public class when_storing_concept_compliance
        {
            [Fact]
            public async Task should_round_trip_protected_values_and_withhold_causation()
            {
                using var scenario = new CommandScenario<RecordInformation>();
                var id = new InvoiceId(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                var result = await scenario.Execute(new RecordInformation(id, new HealthInformation("well"), new ApiKey("token"), new PersonalSecret("private"), InvoiceStatus.Draft));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var appended = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<InformationRecorded>(appended.Content);
                Assert.Equal("well", payload.Health.Value);
                Assert.Equal("token", payload.Key.Value);
                Assert.Equal("private", payload.Note.Value);
                Assert.NotEmpty(appended.Context.Causation);
                var causation = JsonSerializer.Serialize(appended.Context.Causation);
                Assert.DoesNotContain("token", causation);
                Assert.DoesNotContain("well", causation);
                Assert.DoesNotContain("private", causation);
                var projection = new ReadModelScenario<Information>();
                await projection.ProcessEventsFor(id, new object[] { payload });
                Assert.Equal("well", projection.Instance!.Health.Value);
                Assert.Equal("token", projection.Instance.ApiKey.Value);
                Assert.Equal("private", projection.Instance.Note.Value);
            }
        }
        #endif
        """;

    string _build = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_concept_compliance_model.Source + "\n" + a_concept_compliance_model.Lookup));

    async Task Because()
    {
        AddGeneratedSpecification("when_storing_concept_compliance.cs", Probe);
        _build = await Run("concept-compliance-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _tests = await Run("concept-compliance-tests.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact] void should_build_with_the_chronicle_identity_analyzers() => BuildWarnings(_build).ShouldEqual(string.Empty);
    [Fact] void should_round_trip_and_project_values_without_leaking_causation() => _tests.ShouldContain("Passed!");
}
#endif
