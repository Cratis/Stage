// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandResponseContracts;

public class when_building_type_contracts : given.an_enumeration_response
{
    [Theory]
    [InlineData(SemanticPrimitiveType.Boolean, "boolean")]
    [InlineData(SemanticPrimitiveType.WholeNumber, "number")]
    [InlineData(SemanticPrimitiveType.DecimalNumber, "number")]
    [InlineData(SemanticPrimitiveType.Uuid, "string")]
    [InlineData(SemanticPrimitiveType.Text, "string")]
    [InlineData(SemanticPrimitiveType.Date, "string")]
    [InlineData(SemanticPrimitiveType.DateTime, "string")]
    public void should_preserve_primitive_and_concept_transport_kinds(SemanticPrimitiveType primitive, string kind)
    {
        var concept = Application.Concepts[0];
        var application = Application with { Concepts = [concept with { Primitive = primitive }] };
        Assert.Equal(kind, new CommandResponseContracts(application).From([], name => name, property => property.Name)["commands"]["Register"]["type"]["kind"].GetValue<string>());
        var type = ResponseType() with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = primitive };
        Assert.True(JsonNode.DeepEquals(Build(type)["commands"]["Register"], JsonNode.Parse($$$"""{"kind":"value","type":{"kind":"{{{kind}}}"}}""")));
    }

    [Fact]
    public void should_wrap_a_nullable_collection_outside_its_item()
    {
        var type = ResponseType() with { IsCollection = true, IsOptional = true };
        Assert.True(JsonNode.DeepEquals(Build(type)["commands"]["Register"], JsonNode.Parse("""{"kind":"value","type":{"kind":"array","item":{"kind":"string"},"nullable":true}}""")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_keep_unsupported_response_and_input_metadata_null(bool primitive)
    {
        var type = primitive
            ? ResponseType() with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = (SemanticPrimitiveType)99 }
            : ResponseType() with { Kind = (SemanticTypeReferenceKind)99 };
        var contracts = Build(type);
        Assert.Null(contracts["commands"]["Register"]);
        Assert.True(contracts["commandInputs"]["Register"].AsObject().ContainsKey("status"));
        Assert.Null(contracts["commandInputs"]["Register"]["status"]);
    }

    [Fact]
    public void should_refuse_an_entire_record_when_a_field_is_unsupported()
    {
        var command = Application.Modules[0].Features[0].Slices[0].Commands[0];
        var property = command.Properties[0];
        var response = new SemanticRecordCommandResponse([
            new("supported", property.Type, property.Id),
            new("unsupported", property.Type with { Kind = (SemanticTypeReferenceKind)99 }, property.Id)]);
        var names = new List<string>();
        var contracts = new CommandResponseContracts(WithCommand(command with { Response = response })).From(
            [],
            name =>
            {
                names.Add(name);
                return name;
            },
            input => input.Name);
        Assert.Null(contracts["commands"]["Register"]);
        Assert.Equal(new[] { "supported" }, names);
    }

    [Fact]
    public void should_classify_support_before_applying_numeric_enumeration_transport()
    {
        var application = Application with { Concepts = [Application.Concepts[0] with { Primitive = (SemanticPrimitiveType)99 }] };
        Assert.Null(new CommandResponseContracts(application, numericEnumerations: true).From([], name => name, property => property.Name)["commands"]["Register"]);
    }

    SemanticTypeReference ResponseType() => ((SemanticScalarCommandResponse)Application.Modules[0].Features[0].Slices[0].Commands[0].Response).Type;

    JsonObject Build(SemanticTypeReference type)
    {
        var command = Application.Modules[0].Features[0].Slices[0].Commands[0];
        command = command with { Response = ((SemanticScalarCommandResponse)command.Response) with { Type = type }, Properties = [command.Properties[0] with { Type = type }] };
        return new CommandResponseContracts(WithCommand(command)).From([], name => name, property => property.Name);
    }

    SemanticApplication WithCommand(SemanticCommand command)
    {
        var module = Application.Modules[0];
        var feature = module.Features[0];
        var slice = feature.Slices[0] with { Commands = [command] };
        return Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
    }
}
