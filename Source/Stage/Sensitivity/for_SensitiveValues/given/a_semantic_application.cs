// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Sensitivity.for_SensitiveValues.given;

public class a_semantic_application : Specification
{
    protected const string SecretValue = "s3cret-value";

    protected SemanticApplication _application = null!;
    protected SemanticCommand _echo = null!;
    protected SemanticCommand _scalar = null!;
    protected SemanticCommand _record = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Secrets"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("secrets"), "secrets", "Secrets.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Secrets", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _application = compilation.Value.Model.Application;
        var commands = _application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).ToArray();
        _echo = commands.Single(command => command.Name == "Echo");
        _scalar = commands.Single(command => command.Name == "ReturnCredentials");
        _record = commands.Single(command => command.Name == "ReturnRecord");
    }

    protected static Dictionary<string, JsonElement> Payload(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    protected static string Json(object? value) => JsonSerializer.Serialize(value);

    const string Source = """
        concept ProjectId : Uuid
        concept Password : String secret
        concept Note : String
        eventsource Project
          identifier ProjectId
        type Credentials
          note Note
          password Password
        type Wrapper
          label Note
          inner Credentials
        type Node
          name Note
          password Password
          child Node optional
        module M
          feature F
            slice StateChange Echo
              command Echo
                projectId ProjectId identifier
                password Password
                credentials Credentials
                wrapper Wrapper
                history Credentials[]
                node Node
                produces event Echoed
                  for projectId
                  credentials Credentials = credentials
            slice StateChange ReturnCredentials
              command ReturnCredentials
                projectId ProjectId identifier
                credentials Credentials
                produces event CredentialsReturned
                  for projectId
                  credentials Credentials = credentials
                returns credentials
            slice StateChange ReturnRecord
              command ReturnRecord
                projectId ProjectId identifier
                name String
                credentials Credentials
                produces event RecordReturned
                  for projectId
                  name String = name
                returns
                  name = name
                  credentials = credentials
        """;
}
