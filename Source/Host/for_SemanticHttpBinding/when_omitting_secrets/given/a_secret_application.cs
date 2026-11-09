// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_StageEndpointMapper.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_omitting_secrets.given;

public class a_secret_application : a_routed_model
{
    protected const string SecretValue = "s3cret-value";

    protected string _body = string.Empty;
    protected JsonElement _result;
    protected int _status;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Secrets"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("secrets"), "secrets", "Secrets.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Secrets", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Issues));
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan.Plan, appender);
    }

    protected async Task Post(string command, string payload, string suffix = "")
    {
        var response = await Request("POST", $"/api/m/f/{command}/{command}{suffix}", payload);
        _status = response.Status;
        _body = response.Body;
        using var document = JsonDocument.Parse(response.Body);
        _result = document.RootElement.Clone();
    }

    const string Source = """
        concept Username : String
        concept Password : String secret
        concept Note : String
        concept PersonalSecret : String pii secret
        concept StrongPassword : String secret
          validate
            min 20 message "Password is too short"
        policy PublicAccess
          require anonymous
        type Credentials
          note Note
          password Password
        module M
          feature F
            slice StateChange Register
              command Register
                username Username identifier
                password Password
                authorize PublicAccess
                validate
                  username not empty message "Username is required"
                produces event Registered
                  password Password = password
            slice StateChange Enroll
              command Enroll
                username Username identifier
                credentials Credentials
                history Credentials[]
                pins Password[]
                authorize PublicAccess
                produces event Enrolled
                  username Username = username
            slice StateChange Protect
              command Protect
                name String identifier
                token PersonalSecret
                authorize PublicAccess
                produces event Protected
                  name String = name
            slice StateChange ReturnSecret
              command ReturnSecret
                name String identifier
                password Password
                authorize PublicAccess
                produces event SecretReturned
                  name String = name
                returns password
            slice StateChange ReturnRecord
              command ReturnRecord
                name String identifier
                password Password
                authorize PublicAccess
                produces event RecordReturned
                  name String = name
                returns
                  name = name
                  pass_word = password
            slice StateChange Strict
              command Strict
                name String identifier
                password StrongPassword
                authorize PublicAccess
                produces event StrictRegistered
                  name String = name
            slice StateChange Rename
              command Rename
                name String identifier
                note Note
                authorize PublicAccess
                produces event Renamed
                  note Note = note
        """;
}
