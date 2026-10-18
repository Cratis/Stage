// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public class when_describing_generated_inputs_and_responses : for_StageEndpointMapper.given.a_routed_model
{
    StageRoutes _routes = null!;
    JsonElement _schema;
    JsonElement _response;
    int _status;

    async Task Because()
    {
        var plan = compiled_plan.From(Source);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan, appender);
        var scene = new SceneApplication([], [], [], [], [], []);
        _routes = StageRoutes.For(new StageSceneRoutes(scene, _app.Services, NullLogger.Instance), plan.Model);
        using var schema = JsonDocument.Parse(_routes.CommandSchemas["RegisterIdentifier"]);
        _schema = schema.RootElement.Clone();
        var result = await Request("POST", _routes.Commands["RegisterIdentifier"], "{\"name\":\"Apollo\"}");
        _status = result.Status;
        using var response = JsonDocument.Parse(result.Body);
        _response = response.RootElement.Clone();
    }

    [Fact] void should_describe_only_caller_inputs() => _schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).ShouldContainOnly("name");
    [Fact] void should_require_only_caller_inputs() => _schema.GetProperty("required").EnumerateArray().Select(property => property.GetString()).ShouldContainOnly("name");
    [Fact] void should_execute_without_a_caller_supplied_identifier() => _status.ShouldEqual(200);
    [Fact] void should_return_the_generated_identifier_as_a_string() => Guid.Parse(_response.GetProperty("response").GetString()!).ShouldNotEqual(Guid.Empty);
    [Fact] void should_describe_the_generated_identifier_response() => _routes.CommandResponseKinds["RegisterIdentifier"].ShouldEqual("string");
    [Fact] void should_describe_a_text_response() => _routes.CommandResponseKinds["EchoText"].ShouldEqual("string");
    [Fact] void should_describe_a_whole_number_response() => _routes.CommandResponseKinds["EchoCount"].ShouldEqual("number");
    [Fact] void should_describe_a_decimal_response() => _routes.CommandResponseKinds["EchoAmount"].ShouldEqual("number");
    [Fact] void should_describe_a_boolean_response() => _routes.CommandResponseKinds["EchoFlag"].ShouldEqual("boolean");
    [Fact] void should_describe_a_record_response() => _routes.CommandResponseKinds["EchoRecord"].ShouldEqual("object");
    [Fact] void should_describe_the_existing_echo_without_a_declared_response() => _routes.CommandResponseKinds["EchoPayload"].ShouldEqual("object");
    [Fact] void should_publish_the_same_kind_under_the_qualified_identity() => _routes.CommandResponseKinds["Stage.Projects.Registration.Register.RegisterIdentifier"].ShouldEqual("string");

    const string Source = """
        concept ProjectId : Uuid
        concept Amount : Decimal
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterIdentifier
                projectId ProjectId generated identifier
                name String
                produces event IdentifierRegistered
                  name String = name
                returns projectId
            slice StateChange Text
              command EchoText
                value String
                returns value
            slice StateChange Count
              command EchoCount
                value Int
                returns value
            slice StateChange Amount
              command EchoAmount
                value Amount
                returns value
            slice StateChange Flag
              command EchoFlag
                value Bool
                returns value
            slice StateChange Record
              command EchoRecord
                name String
                returns
                  name = name
            slice StateChange Payload
              command EchoPayload
                name String
        """;
}
