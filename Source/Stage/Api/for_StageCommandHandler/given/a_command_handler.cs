// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Runtime;
using NSubstitute;

namespace Cratis.Stage.Api.for_StageCommandHandler.given;

public class a_command_handler : Specification
{
    protected IAppendProducedEvents _appender = null!;
    protected IProvideStageIdentity _identity = null!;
    protected string _eventSourceId = string.Empty;

    void Establish()
    {
        _identity = Substitute.For<IProvideStageIdentity>();
        _identity.Current().Returns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        _appender = Substitute.For<IAppendProducedEvents>();
        _appender
            .Append(Arg.Any<string>(), Arg.Any<IReadOnlyList<ProducedEventPayload>>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(call =>
            {
                _eventSourceId = (string)call[0];
                return Task.CompletedTask;
            });
    }

    protected StageCommandHandler HandlerFor(string? identifier) =>
        new(typeof(DynamicCommand), [], Definition(identifier), _appender, _identity);

    protected static CommandContext ContextFor(string payload) =>
        new(
            CorrelationId.New(),
            typeof(DynamicCommand),
            new DynamicCommand { Data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload)! },
            [],
            CommandContextValues.Empty);

    // The handler only reads the produces declarations and the identifier off the definition - the schemas are the
    // API surface's concern, so they are left empty here.
    static CommandDefinition Definition(string? identifier) =>
        new(
            Guid.Empty,
            "RegisterInvoice",
            string.Empty,
            string.Empty,
            [],
            string.Empty,
            [new ProducedEvent("InvoiceRegistered", null, [], [])],
            identifier);
}
