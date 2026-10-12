// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandCompliance;

public class when_excluding_secret_inputs
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_exclude_secrets_with_any_input_modifiers(bool collection, bool optional)
    {
        var context = Context();
        var key = context.Types.Values.Single(type => type.Name == "Credentials").Properties.Single(property => property.Name == "key").Type;
        var type = key with { IsCollection = collection, IsOptional = optional };
        new SemanticCommandCompliance(context).InputAttribute(type).ShouldContain("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_terminate_cycles_and_find_reachable_secrets(bool secret)
    {
        var context = Context();
        var types = (Dictionary<SemanticId, SemanticCompositeType>)context.Types;
        var credentials = types.Values.Single(type => type.Name == "Credentials");
        var envelope = types.Values.Single(type => type.Name == "CredentialEnvelope");
        var input = context.Commands.Values.Single(command => command.Name == "RecordCredentials").Properties.Single(property => property.Name == "credentials").Type;
        var cycle = credentials.Properties[0] with { Type = input };
        types[credentials.Id] = credentials with { Properties = secret ? [cycle, .. credentials.Properties] : [cycle] };
        types[envelope.Id] = envelope with { Properties = [envelope.Properties[0]] };
        new SemanticCommandCompliance(context).InputAttribute(input).Contains("NotAudited", StringComparison.Ordinal).ShouldEqual(secret);
    }

    [Fact]
    public void should_leave_nonsecret_inputs_unchanged()
    {
        var context = Context();
        var properties = context.Commands.Values.Single(command => command.Name == "RecordInformation").Properties;
        var types = properties.Where(property => property.Name != "key" && property.Name != "note").Select(property => property.Type);
        types.Select(type => new SemanticCommandCompliance(context).InputAttribute(type)).All(string.IsNullOrEmpty).ShouldBeTrue();
    }

    static SemanticApplicationContext Context()
    {
        var source = a_concept_compliance_model.ContainerTypes + "\n" + a_concept_compliance_model.Source + "\n" + a_concept_compliance_model.ContainerSlice;
        var model = invoice_model.Compile(source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));

        return new(request, options);
    }
}
