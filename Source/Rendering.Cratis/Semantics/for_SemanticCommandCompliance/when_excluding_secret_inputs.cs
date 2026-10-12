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
    [InlineData("key", false, false)]
    [InlineData("key", false, true)]
    [InlineData("key", true, false)]
    [InlineData("key", true, true)]
    [InlineData("health", false, false)]
    [InlineData("health", false, true)]
    [InlineData("health", true, false)]
    [InlineData("health", true, true)]
    public void should_exclude_protected_values_with_any_input_modifiers(string property, bool collection, bool optional)
    {
        var context = Context();
        var value = context.Types.Values.Single(type => type.Name == "Credentials").Properties.Single(member => member.Name == property).Type;
        var type = value with { IsCollection = collection, IsOptional = optional };
        new SemanticCommandCompliance(context).InputAttribute(type).ShouldContain("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]");
    }

    [Theory]
    [InlineData("credentials")]
    [InlineData("optionalCredentials")]
    [InlineData("healthDetails")]
    [InlineData("optionalHealthDetails")]
    public void should_exclude_composites_with_nested_protected_values(string property)
    {
        var context = Context();
        var input = context.Commands.Values.Single(command => command.Name == "RecordCredentials").Properties.Single(member => member.Name == property).Type;
        new SemanticCommandCompliance(context).InputAttribute(input).ShouldContain("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]");
    }

    [Theory]
    [InlineData("key", true)]
    [InlineData("key", false)]
    [InlineData("health", true)]
    [InlineData("health", false)]
    public void should_terminate_cycles_and_find_reachable_protected_values(string property, bool protectedValue)
    {
        var context = Context();
        var types = (Dictionary<SemanticId, SemanticCompositeType>)context.Types;
        var credentials = types.Values.Single(type => type.Name == "Credentials");
        var envelope = types.Values.Single(type => type.Name == "CredentialEnvelope");
        var input = context.Commands.Values.Single(command => command.Name == "RecordCredentials").Properties.Single(property => property.Name == "credentials").Type;
        var cycle = credentials.Properties[0] with { Type = input };
        types[credentials.Id] = credentials with { Properties = protectedValue ? [cycle, credentials.Properties.Single(member => member.Name == property)] : [cycle] };
        types[envelope.Id] = envelope with { Properties = [envelope.Properties[0]] };
        new SemanticCommandCompliance(context).InputAttribute(input).Contains("NotAudited", StringComparison.Ordinal).ShouldEqual(protectedValue);
    }

    [Fact]
    public void should_leave_unprotected_inputs_unchanged()
    {
        var context = Context();
        var properties = context.Commands.Values.Single(command => command.Name == "RecordInformation").Properties;
        var types = properties.Where(property => property.Name == "id" || property.Name == "status").Select(property => property.Type);
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
