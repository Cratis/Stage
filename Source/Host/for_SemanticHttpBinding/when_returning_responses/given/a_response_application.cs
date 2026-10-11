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

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_returning_responses.given;

public class a_response_application : a_routed_model
{
    protected readonly List<SemanticFact> _facts = [];
    protected JsonElement _result;
    protected int _status;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("responses"), "responses", "Responses.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Issues));
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        appender.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(call =>
        {
            _facts.AddRange(call.Arg<IReadOnlyList<SemanticFact>>());
            return Task.CompletedTask;
        });
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan.Plan!, appender);
    }

    protected async Task Post(string slice, string command, string payload)
    {
        var response = await Request("POST", $"/api/projects/registration/{slice}/{command}", payload);
        _status = response.Status;
        using var document = JsonDocument.Parse(response.Body);
        _result = document.RootElement.Clone();
    }

    protected string AppendedId() => ((SemanticTextValue)_facts.Single().Destination).Value;

    protected bool HasNoResponse() => !_result.TryGetProperty("response", out var response) || response.ValueKind == JsonValueKind.Null;

    const string Source = """
        concept ProjectId : Uuid
        policy Staff
          require role "Staff"
        module Projects
          feature Registration
            slice StateChange RegisterIdentifier
              command RegisterIdentifier
                projectId ProjectId generated identifier
                name String
                validate
                  name not empty message "Project name is required"
                produces event IdentifierRegistered
                  name String = name
                returns projectId
            slice StateChange RegisterRecord
              command RegisterRecord
                projectId ProjectId generated identifier
                name String
                produces event RecordRegistered
                  name String = name
                returns
                  projectId = projectId
                  name = name
            slice StateChange RegisterRestricted
              command RegisterRestricted
                projectId ProjectId generated identifier
                name String
                authorize Staff
                produces event RestrictedRegistered
                  name String = name
                returns projectId
            slice StateChange EchoProject
              command EchoProject
                projectId ProjectId identifier
                name String
                produces event ProjectEchoed
                  name String = name
            slice StateChange ReturnOptional
              command ReturnOptional
                value Int?
                returns value
        """;
}
