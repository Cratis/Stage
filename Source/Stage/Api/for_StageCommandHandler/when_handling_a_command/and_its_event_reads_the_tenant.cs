// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Runtime;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_its_event_reads_the_tenant : given.a_command_handler
{
    readonly Dictionary<string, string> _values = [];
    readonly List<Exception> _errors = [];
    int _appended;
    string _value = string.Empty;
    int _appendedBeforeCollisions;

    void Establish() => _appender.Append(Arg.Any<string>(), Arg.Any<IReadOnlyList<ProducedEventPayload>>(), Arg.Any<IReadOnlyDictionary<string, string>>())
        .Returns(call =>
        {
            _appended++;
            _value = ((IReadOnlyList<ProducedEventPayload>)call[1])[0].Content["tenant"]!.GetValue<string>();
            return Task.FromResult<CommandResult?>(null);
        });

    async Task Because()
    {
        var handler = HandlerProducing(new ProducedEvent("InvoiceRegistered", null, [new("tenant", ProducedValueKind.Tenant, string.Empty)], []));
        foreach (var name in new[] { TenantId.Default.Value, TenantId.NotSet.Value, "North", "default" })
        {
            _tenants.Current.Returns(new TenantId(name));
            await handler.Handle(ContextFor("{\"invoiceId\":\"invoice\"}"));
            _values[name] = _value;
        }
        _appendedBeforeCollisions = _appended;
        foreach (var name in new[] { string.Empty, "00000000-0000-0000-0000-000000000000" })
        {
            _tenants.Current.Returns(new TenantId(name));
            _errors.Add(await Catch.Exception(() => handler.Handle(ContextFor("{\"invoiceId\":\"invoice\"}")).AsTask()));
        }
    }

    [Fact] void should_translate_default_to_the_portable_zero_guid() => _values[TenantId.Default.Value].ShouldEqual(PortableTenantValues.Default);
    [Fact] void should_translate_not_set_to_the_portable_empty_sentinel() => _values[TenantId.NotSet.Value].ShouldEqual(PortableTenantValues.NotSet);
    [Fact] void should_preserve_a_named_tenant() => _values["North"].ShouldEqual("North");
    [Fact] void should_preserve_named_tenant_casing() => _values["default"].ShouldEqual("default");
    [Fact] void should_reject_collisions() => _errors.TrueForAll(error => error is AmbiguousTenant).ShouldBeTrue();
    [Fact] void should_not_append_for_an_ambiguous_tenant() => _appended.ShouldEqual(_appendedBeforeCollisions);
}
