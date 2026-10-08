// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration.given;

public class a_chronicle_registration : a_stored_sequence
{
    protected IChronicleClient _client = null!;
    protected readonly List<StageUnsupportedIssue> _mirrorIssues = [];
    protected readonly List<string> _registrations = [];
    protected RegisterManyRequest? _models;
    protected RegisterRequest? _projections;
    protected SemanticWorld _world = null!;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        var store = Substitute.For<IEventStore>();
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(_services);
        store.Name.Returns((EventStoreName)"Projects");
        store.Connection.Returns(connection);
        _client.GetEventStore("Projects").Returns(store);
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(Chronicle.Contracts.Commands.CommandResult.Success(Guid.Empty));
        _services.EventTypes.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()).Returns(_ =>
        {
            _registrations.Add("events");
            return Chronicle.Contracts.Commands.CommandResult.Success(Guid.Empty);
        });
        _services.ReadModels.When(service => service.RegisterMany(Arg.Any<RegisterManyRequest>())).Do(call =>
        {
            _models = call.Arg<RegisterManyRequest>();
            _registrations.Add("models");
        });
        _services.Projections.When(service => service.Register(Arg.Any<RegisterRequest>())).Do(call =>
        {
            _projections = call.Arg<RegisterRequest>();
            _registrations.Add("projections");
        });
    }

    protected void UseSource(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("project-model"), "project-model", "RegisterProject.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compiled.Value!.Model);
        Assert.True(plan.Success, string.Join("; ", plan.Issues.Select(issue => issue.Details)));
        _plan = plan.Plan!;
    }
}
