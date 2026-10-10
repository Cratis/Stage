// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public class a_routed_runtime : Specification
{
    internal const string Source = """
        concept AccountId : String
        concept Period : String
        policy Owner
          require authenticated
        eventsource Account
          id "stored-account"
          identifier AccountId
          stream Transactions
            id "stored-transactions"
            streamId Period
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                period Period
                amount Int
                authorize Owner
                stream Account.Transactions
                  streamId = period
                produces Deposited
                  for accountId
                  amount = amount
              event Deposited
                amount Int
        """;

    protected SemanticExecutionPlan _plan = null!;
    protected ISemanticRuntime _runtime = null!;
    protected IServices _contracts = null!;
    protected AppendManyForEventSourcesRequest? _append;
    protected ClaimsPrincipal _principal = new(new ClaimsIdentity([], "fixture"));
    protected string _period = "2026-10";
    ServiceProvider _provider = null!;

    protected virtual string ModelSource => Source;

    void Establish()
    {
        _plan = compiled_plan.From(ModelSource);
        _contracts = Substitute.For<IServices>();
        _contracts.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = ulong.MaxValue }));
        _contracts.Sequences.AppendManyForEventSources(Arg.Do<AppendManyForEventSourcesRequest>(request => _append = request)).Returns(Cratis.Chronicle.Contracts.Commands.CommandResult<AppendManyResponse>.Success(Guid.Empty, new() { IsSuccess = true }));
        var connection = Substitute.For<Cratis.Chronicle.Connections.IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(_contracts);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("Routed"));
        store.Connection.Returns(connection);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(store);
        var services = new ServiceCollection();
        services.AddSingleton(client);
        services.AddSingleton(new StageEventStoreName("Routed"));
        SemanticRuntimeHosting.Add(services, _plan);
        _provider = services.BuildServiceProvider();
        _runtime = _provider.GetRequiredService<ISemanticRuntime>();
    }

    protected Task<SemanticExecutionResult> Execute() => _runtime.Execute(
        _plan.Commands.Values.Single(),
        new Dictionary<string, JsonElement>
        {
            ["accountId"] = JsonSerializer.SerializeToElement("acc-1"),
            ["period"] = JsonSerializer.SerializeToElement(_period),
            ["amount"] = JsonSerializer.SerializeToElement(10)
        },
        _principal,
        new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), "owner", "Owner", "owner"),
        false);

    void Destroy() => _provider.Dispose();
}
