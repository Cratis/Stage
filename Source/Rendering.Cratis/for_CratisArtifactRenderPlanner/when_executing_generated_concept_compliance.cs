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
        using Invoices.Invoices.Lifecycle.RecordCredentials;
        using Invoices.Invoices.Lifecycle.RecordSecrets;
        using Xunit;

        namespace Invoices.ComplianceProbe;

        public class when_storing_concept_compliance
        {
            [Fact]
            public async Task should_round_trip_protected_values_and_withhold_causation()
            {
                using var scenario = new CommandScenario<RecordInformation>();
                var id = new InvoiceId(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                var result = await scenario.Execute(new RecordInformation(id, new HealthInformation("well"), new ApiKey("scalar-secret-value-345"), new PersonalSecret("private"), InvoiceStatus.Draft));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var appended = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<InformationRecorded>(appended.Content);
                Assert.Equal("well", payload.Health.Value);
                Assert.Equal("scalar-secret-value-345", payload.Key.Value);
                Assert.Equal("private", payload.Note.Value);
                Assert.NotEmpty(appended.Context.Causation);
                var causation = JsonSerializer.Serialize(appended.Context.Causation);
                Assert.DoesNotContain(payload.Key.Value, causation);
                Assert.All(appended.Context.Causation, cause => Assert.False(cause.Properties.ContainsKey("key")));
                Assert.DoesNotContain("well", causation);
                Assert.DoesNotContain("private", causation);
                var projection = new ReadModelScenario<Information>();
                await projection.ProcessEventsFor(id, new object[] { payload });
                Assert.Equal("well", projection.Instance!.Health.Value);
                Assert.Equal("scalar-secret-value-345", projection.Instance.ApiKey.Value);
                Assert.Equal("private", projection.Instance.Note.Value);
            }

            [Fact]
            public async Task should_append_all_non_text_secrets_without_leaking_causation()
            {
                using var scenario = new CommandScenario<RecordSecrets>();
                var id = new InvoiceId(Guid.NewGuid());
                var date = new DateOnly(2026, 10, 21);
                var time = new DateTimeOffset(2026, 10, 21, 12, 30, 0, TimeSpan.Zero);
                var uuid = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
                var result = await scenario.Execute(new RecordSecrets(id, new SecretNumber(37), new SecretFlag(true), new SecretAmount(37.25m), new SecretDate(date), new SecretTime(time), new SecretUuid(uuid)));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var appended = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<SecretsRecorded>(appended.Content);
                Assert.Equal(37, payload.Number.Value);
                Assert.True(payload.Flag.Value);
                Assert.Equal(37.25m, payload.Amount.Value);
                Assert.Equal(date, payload.Date.Value);
                Assert.Equal(time, payload.Time.Value);
                Assert.Equal(uuid, payload.Uuid.Value);
                Assert.NotEmpty(appended.Context.Causation);
                foreach (var property in new[] { "number", "flag", "amount", "date", "time", "uuid" })
                {
                    Assert.All(appended.Context.Causation, cause => Assert.False(cause.Properties.ContainsKey(property)));
                }
            }

            [Fact]
            public async Task should_withhold_collection_and_nested_optional_composite_inputs()
            {
                using var scenario = new CommandScenario<RecordCredentials>();
                var id = new InvoiceId(Guid.NewGuid());
                var credentials = new CredentialEnvelope(
                    new[] { new Credentials(new ApiKey("nested-secret-value-345"), new HealthInformation("nested-personal-value-345")) },
                    new ApiKey("optional-secret-value-345"));
                var healthDetails = new HealthEnvelope(
                    new[] { new HealthInformation("nested-pii-only-value-345") },
                    new HealthInformation("optional-pii-only-value-345"));
                var result = await scenario.Execute(new RecordCredentials(id, new[] { new ApiKey("collection-secret-value-345") }, credentials, credentials,
                    new[] { new HealthInformation("collection-pii-only-value-345") }, healthDetails, healthDetails, "auditable-value-345"));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var appended = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<CredentialsRecorded>(appended.Content);
                Assert.Equal("collection-secret-value-345", Assert.Single(payload.Keys).Value);
                var entry = Assert.Single(payload.Credentials!.Entries);
                Assert.Equal("nested-secret-value-345", entry.Key.Value);
                Assert.Equal("nested-personal-value-345", entry.Health.Value);
                Assert.Equal("optional-secret-value-345", payload.Credentials.OptionalKey!.Value);
                Assert.Equal("collection-pii-only-value-345", Assert.Single(payload.Healths).Value);
                Assert.Equal("nested-pii-only-value-345", Assert.Single(payload.HealthDetails!.Entries).Value);
                Assert.Equal("optional-pii-only-value-345", payload.HealthDetails.OptionalHealth!.Value);
                Assert.NotEmpty(appended.Context.Causation);
                var causation = JsonSerializer.Serialize(appended.Context.Causation);
                foreach (var value in new[] { payload.Keys[0].Value, entry.Key.Value, entry.Health.Value, payload.Credentials.OptionalKey.Value,
                    payload.Healths[0].Value, payload.HealthDetails.Entries[0].Value, payload.HealthDetails.OptionalHealth.Value })
                {
                    Assert.DoesNotContain(value, causation);
                }
                foreach (var property in new[] { "keys", "credentials", "optionalCredentials", "healths", "healthDetails", "optionalHealthDetails" })
                {
                    Assert.All(appended.Context.Causation, cause => Assert.False(cause.Properties.ContainsKey(property)));
                }
                Assert.Contains(appended.Context.Causation, cause => cause.Properties.TryGetValue("visible", out var value) && value == "auditable-value-345");
            }
        }
        #endif
        """;

    const string NonTextSecrets = """
            slice StateChange RecordSecrets
              command RecordSecrets
                id InvoiceId identifier
                number SecretNumber
                flag SecretFlag
                amount SecretAmount
                date SecretDate
                time SecretTime
                uuid SecretUuid
                produces SecretsRecorded
                  for id
                  number = number
                  flag = flag
                  amount = amount
                  date = date
                  time = time
                  uuid = uuid
              event SecretsRecorded
                number SecretNumber
                flag SecretFlag
                amount SecretAmount
                date SecretDate
                time SecretTime
                uuid SecretUuid
        """;

    string _build = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile("concept SecretNumber : Int secret\nconcept SecretFlag : Bool secret\nconcept SecretAmount : Decimal secret\nconcept SecretDate : Date secret\nconcept SecretTime : DateTime secret\nconcept SecretUuid : Uuid secret\n" + a_concept_compliance_model.ContainerTypes + "\n" + a_concept_compliance_model.Source + "\n" + a_concept_compliance_model.ContainerSlice + "\n" + NonTextSecrets + "\n" + a_concept_compliance_model.Lookup));

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
