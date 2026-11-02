// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_a_given_only_source_key_specification : given.a_translation_application
{
    ExecutableSemanticModel Canonical => SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan());

    [Fact]
    async Task should_project_the_unchanged_canonical_private_given()
    {
        _model = Canonical;
        var spec = Translation().Specifications.Single();
        spec.Id.ToString().ShouldEqual("sem1:450a348c26380bd109150021f55c569605dbaef3cf4c29735bbc8d17391bc396");
        spec.Name.ShouldEqual("AReceivedOrderIsSourcedByItsKey");
        spec.When.ShouldBeNull();
        spec.WhenAppended.ShouldBeNull();
        spec.GivenEvents.Single().EventContract.ShouldEqual(Translation().Events.Single(contract => contract.Origin is null).Id);
        spec.GivenEvents.Single().EventSource.ShouldNotBeNull();
        spec.ThenReadModels.Single().ReadModel.ShouldEqual(_model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView).ReadModels.Single().Id);
        var files = AdmittedFiles(Plan(_model), spec);
        (await RunProjected(files)).ShouldBeGreaterThan(0);
    }

    [Fact]
    void should_refuse_a_wrong_key_at_admission() => Refuse(spec => spec with
    {
        ThenReadModels = [spec.ThenReadModels.Single() with
        {
            Key = new SemanticTextValue("9fa85f64-5717-4562-b3fc-2c963f66afa9"),
            Values = [spec.ThenReadModels.Single().Values.Single() with { Value = new SemanticTextValue("9fa85f64-5717-4562-b3fc-2c963f66afa9") }]
        }]
    });

    [Fact]
    void should_reject_an_inconsistent_identifier_before_rendering()
    {
        _model = Canonical;
        Assert.Throws<InvalidSemanticContract>(() => Transform(_model, slice => slice.Kind != SemanticSliceKind.Translate ? slice : slice with
        {
            Specifications = [slice.Specifications.Single() with
            {
                ThenReadModels = [slice.Specifications.Single().ThenReadModels.Single() with
                {
                    Values = [slice.Specifications.Single().ThenReadModels.Single().Values.Single() with { Value = new SemanticTextValue("9fa85f64-5717-4562-b3fc-2c963f66afa9") }]
                }]
            }]
        }));
    }

    [Fact]
    void should_refuse_foreign_given_settlement() => Refuse(spec => spec with
    {
        GivenEvents = [spec.GivenEvents.Single() with
        {
            EventContract = Translation().Events.Single(contract => contract.Origin is not null).Id,
            Values = [new(Translation().Events.Single(contract => contract.Origin is not null).Properties.Single().Id, spec.GivenEvents.Single().Values.Single().Value)]
        }]
    });

    [Fact]
    void should_refuse_mixed_event_outcomes() => Refuse(spec => spec with { ThenEvents = spec.GivenEvents });

    [Fact]
    void should_refuse_an_append_action() => Refuse(spec => spec with { WhenAppended = new(spec.GivenEvents.Single().EventContract, spec.GivenEvents.Single().Values) { EventSource = spec.GivenEvents.Single().EventSource } });

    [Fact]
    void should_refuse_missing_source() => Refuse(spec => spec with { GivenEvents = [spec.GivenEvents.Single() with { EventSource = null }] });

    [Fact]
    void should_refuse_routed_history() => Refuse(spec => spec with { GivenEvents = [spec.GivenEvents.Single() with { Route = new(_model.Application.Id, _model.Application.Id) }] });

    [Fact]
    void should_refuse_multiple_read_expectations() => Refuse(spec => spec with { ThenReadModels = [spec.ThenReadModels.Single(), spec.ThenReadModels.Single()] });

    [Fact]
    void should_refuse_invocation_outcomes() => Refuse(spec => spec with { ThenInvocations = [new(_model.Application.Id, [])] });

    [Fact]
    void should_refuse_invocations_in_an_otherwise_supported_direct_append()
    {
        _model = Canonical;
        var slice = Translation();
        var original = slice.Specifications.Single();
        var foreign = slice.Events.Single(contract => contract.Origin is not null);
        var spec = original with
        {
            GivenEvents = [],
            ThenReadModels = [],
            ThenEvents = original.GivenEvents,
            WhenAppended = new(foreign.Id, [new(foreign.Properties.Single().Id, original.GivenEvents.Single().Values.Single().Value)])
        };
        var context = Context();
        SemanticTranslationSpecificationAdmission.CannotRender(context, slice, spec).ShouldBeNull();
        SemanticTranslationSpecificationAdmission.CannotRender(context, slice, spec with { ThenInvocations = [new(_model.Application.Id, [])] }).ShouldNotBeNull();
    }

    [Fact]
    void should_record_the_refusal_and_omit_a_wrong_key_specification()
    {
        _model = Canonical;
        var original = Translation().Specifications.Single();
        var model = Transform(_model, slice => slice.Kind == SemanticSliceKind.Translate ? slice with
        {
            Specifications = [original with
            {
                ThenReadModels = [original.ThenReadModels.Single() with
                {
                    Key = new SemanticTextValue("9fa85f64-5717-4562-b3fc-2c963f66afa9"),
                    Values = [original.ThenReadModels.Single().Values.Single() with { Value = new SemanticTextValue("9fa85f64-5717-4562-b3fc-2c963f66afa9") }]
                }]
            }]
        } : slice);
        var plan = Plan(model);
        Assert.Contains(plan.Refusals, refusal => refusal.Artifact == original.Id && refusal.Codes.Contains("STAGE-ESM-011"));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.Sources.Contains(original.Id));
    }

    [Fact]
    async Task should_project_a_required_non_key_payload_in_a_separate_derived_fixture()
    {
        var model = PayloadModel();
        var spec = PayloadSpecification(model);
        (await RunProjected(AdmittedFiles(Plan(model), spec))).ShouldBeGreaterThan(0);
    }

    [Fact]
    async Task should_fail_the_generated_property_assertion_for_a_wrong_payload()
    {
        var model = PayloadModel();
        var original = PayloadSpecification(model);
        var expected = original.ThenReadModels.Single();
        var payload = expected.Values.Single(value => value.Value is SemanticTextValue text && text.Value.Equals("actual", StringComparison.Ordinal));
        model = Transform(model, slice => slice.Kind == SemanticSliceKind.Translate ? slice with
        {
            Specifications = [original with
            {
                ThenReadModels = [expected with { Values = [.. expected.Values.Select(value => value == payload ? value with { Value = new SemanticTextValue("wrong") } : value)] }]
            }]
        } : slice);
        var files = AdmittedFiles(Plan(model), PayloadSpecification(model));
        (await RunProjected(files, "should_project_payload")).ShouldBeGreaterThan(0);
    }

    // Separate derived v33 fixture; the unchanged canonical proof above remains identity-only.
    ExecutableSemanticModel PayloadModel()
    {
        var model = Canonical;
        var slices = model.Application.Modules.Single().Features.Single().Slices;
        var received = slices.Single(slice => slice.Kind == SemanticSliceKind.Translate).Events.Single(contract => contract.Origin is null);
        var readModel = slices.Single(slice => slice.Kind == SemanticSliceKind.StateView).ReadModels.Single();
        var eventPayload = received.Properties.Single() with
        {
            Id = SemanticId.Create(SemanticKind.Property, "given-only-event-payload"),
            Name = "payload",
            Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
            IsIdentifier = false
        };
        var readPayload = eventPayload with { Id = SemanticId.Create(SemanticKind.Property, "given-only-read-payload") };
        return Transform(model, slice => slice.Kind == SemanticSliceKind.Translate ? slice with
        {
            Events = [.. slice.Events.Select(contract => contract.Id == received.Id ? contract with { Properties = [.. contract.Properties, eventPayload] } : contract)],
            Captures = [.. slice.Captures.Select(capture => capture with
            {
                Appends = [.. capture.Appends.Select(append => append with
                {
                    Mappings = [.. append.Mappings, new(eventPayload.Id) { Value = new SemanticValueExpression(new SemanticTextValue("actual")) }]
                })]
            })],
            Specifications = [.. slice.Specifications.Select(spec => spec with
            {
                GivenEvents = [spec.GivenEvents.Single() with { Values = [.. spec.GivenEvents.Single().Values, new(eventPayload.Id, new SemanticTextValue("actual"))] }],
                ThenReadModels = [spec.ThenReadModels.Single() with { Values = [.. spec.ThenReadModels.Single().Values, new(readPayload.Id, new SemanticTextValue("actual"))] }]
            })]
        } : slice with
        {
            ReadModels = [readModel with { Properties = [.. readModel.Properties, readPayload] }],
            Projections = [.. slice.Projections.Select(projection => projection with
            {
                Scope = projection.Scope with
                {
                    From = [.. projection.Scope.From.Select(binding => binding.EventContract == received.Id ? binding with
                    {
                        Mappings = [.. binding.Mappings, new([readPayload.Id], SemanticProjectionOperation.Set, new SemanticProjectionEventProperty([eventPayload.Id]))]
                    } : binding)]
                }
            })]
        });
    }

    static SemanticSpecification PayloadSpecification(ExecutableSemanticModel model) => model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate).Specifications.Single();

    static RenderedFile[] AdmittedFiles(ArtifactRenderPlan plan, SemanticSpecification spec)
    {
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.DoesNotContain(plan.Refusals, refusal => refusal.Artifact == spec.Id);
        Assert.Contains(plan.Artifacts, artifact => artifact.Sources.Contains(spec.Id) && Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("_is_projected", StringComparison.Ordinal));
        var files = when_rendering_read_model_absence.Files(plan);
        string.Join(Environment.NewLine, RenderedOutput.Errors(files)).ShouldEqual(string.Empty);
        string.Join(Environment.NewLine, RenderedOutput.Warnings(files)).ShouldEqual(string.Empty);
        return files;
    }

    SemanticSlice Translation() => _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate);

    void Refuse(Func<SemanticSpecification, SemanticSpecification> change)
    {
        _model = Canonical;
        var original = Translation().Specifications.Single();
        SemanticTranslationSpecificationAdmission.CannotRender(Context(), Translation(), change(original)).ShouldNotBeNull();
    }

    SemanticApplicationContext Context()
    {
        var options = new CratisRenderingOptions("Contracts", "Contracts");
        var request = new ArtifactRenderRequest(
            _model,
            SemanticExecutionPlan.Compile(_model).Plan,
            CratisRendering.CreateProfile(_model.Application.Name, options),
            new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        return new(request, options);
    }

    static async Task<int> RunProjected(RenderedFile[] files, string? failingFact = null)
    {
        string Qualified(string name)
        {
            var file = files.Single(file => file.Content.Contains($"record {name}(", StringComparison.Ordinal) || file.Content.Contains($"class {name} :", StringComparison.Ordinal));
            var declaration = file.Content.Split('\n').Single(line => line.StartsWith("namespace ", StringComparison.Ordinal));
            return $"global::{declaration[10..].TrimEnd(';', '\r')}.{name}";
        }
        var artifacts = $" _scenario = new(default, new global::Cratis.Chronicle.Testing.Defaults(new global::Cratis.Chronicle.Testing.InMemoryClientArtifactsProvider {{ EventTypes = [typeof({Qualified("OrderReceived")})], Projections = [typeof({Qualified("OrderStateProjection")})] }}));";
        var assembly = when_rendering_read_model_absence.LoadWithArtifacts(files, artifacts);
        var types = assembly.GetTypes().Where(type => type.Name.Contains("_is_projected", StringComparison.Ordinal)).ToArray();
        types.ShouldNotBeEmpty();
        var count = 0;
        foreach (var type in types)
        {
            using var instance = (IDisposable)Activator.CreateInstance(type);
            await (Task)type.GetMethod("Establish", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, null);
            var facts = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(method => Attribute.IsDefined(method, typeof(FactAttribute))).ToArray();
            facts.ShouldNotBeEmpty();
            foreach (var fact in facts)
            {
                failingFact = InvokeFact(fact, instance, failingFact);
                count++;
            }
        }
        failingFact.ShouldBeNull();
        return count;
    }

    static string? InvokeFact(MethodInfo fact, object instance, string? failingFact)
    {
        if (fact.Name == failingFact)
        {
            var exception = Assert.Throws<TargetInvocationException>(() => fact.Invoke(instance, null));
            Assert.IsType<Xunit.Sdk.XunitException>(exception.InnerException, exactMatch: false);
            return null;
        }
        fact.Invoke(instance, null);
        return failingFact;
    }
}
#endif
