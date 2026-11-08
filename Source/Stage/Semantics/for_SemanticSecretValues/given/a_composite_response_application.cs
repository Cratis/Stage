// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSecretValues.given;

public class a_composite_response_application : Specification
{
    protected SemanticApplication _application = null!;
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
        _scalar = commands.Single(command => command.Name == "ReturnCredentials");
        _record = commands.Single(command => command.Name == "ReturnRecord");
    }

    protected static Dictionary<string, object?> Credentials(string password) => new() { ["note"] = "main", ["password"] = password };

    const string Source = """
        concept ProjectId : Uuid
        concept Password : String secret
        concept Note : String
        eventsource Project
          identifier ProjectId
        type Credentials
          note Note
          password Password
        module M
          feature F
            slice StateChange ReturnCredentials
              command ReturnCredentials
                projectId ProjectId identifier
                credentials Credentials
                history Credentials[]
                produces event CredentialsReturned
                  for projectId
                  credentials Credentials = credentials
                returns credentials
            slice StateChange ReturnRecord
              command ReturnRecord
                projectId ProjectId identifier
                name String
                credentials Credentials
                history Credentials[]
                produces event RecordReturned
                  for projectId
                  name String = name
                returns
                  name = name
                  credentials = credentials
        """;
}
