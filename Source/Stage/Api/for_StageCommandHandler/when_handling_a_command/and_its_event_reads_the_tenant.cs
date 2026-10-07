// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Runtime;
using Microsoft.Extensions.Options;
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
    string _withoutHeader = string.Empty;

    void Establish() => _appender.Append(Arg.Any<string>(), Arg.Any<IReadOnlyList<ProducedEventPayload>>(), Arg.Any<IReadOnlyDictionary<string, string>>())
        .Returns(call =>
        {
            _appended++;
            _value = ((IReadOnlyList<ProducedEventPayload>)call[1])[0].Content["tenant"]!.GetValue<string>();
            return Task.FromResult<CommandResult?>(null);
        });

    async Task Because()
    {
        var produced = new ProducedEvent("InvoiceRegistered", null, [new("tenant", ProducedValueKind.Tenant, string.Empty)], []);
        var handler = HandlerProducing(produced);
        foreach (var name in new[] { TenantId.Default.Value, TenantId.NotSet.Value, "North", "default" })
        {
            _tenants.Current.Returns(new TenantId(name));
            await handler.Handle(ContextFor("{\"invoiceId\":\"invoice\"}"));
            _values[name] = _value;
        }
        var request = Substitute.For<IHttpRequestContext>();
        request.Headers.Returns(new Dictionary<string, string>());
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(request);
        var headerTenant = new TenantIdAccessor(new HeaderTenantIdResolver(requests, Options.Create(new ArcOptions())));
        await HandlerProducing(produced, headerTenant).Handle(ContextFor("{\"invoiceId\":\"invoice\"}"));
        _withoutHeader = _value;
        _appendedBeforeCollisions = _appended;
        foreach (var name in new[] { string.Empty, "00000000-0000-0000-0000-000000000000" })
        {
            _tenants.Current.Returns(new TenantId(name));
            _errors.Add(await Catch.Exception(() => handler.Handle(ContextFor("{\"invoiceId\":\"invoice\"}")).AsTask()));
        }
    }

    [Fact] void should_translate_default_to_the_portable_zero_guid() => _values[TenantId.Default.Value].ShouldEqual(PortableTenantValues.Default);
    [Fact] void should_translate_arcs_unset_tenant_to_the_portable_default() => _values[TenantId.NotSet.Value].ShouldEqual(PortableTenantValues.Default);
    [Fact] void should_use_the_portable_default_for_a_request_without_a_tenant_header() => _withoutHeader.ShouldEqual(PortableTenantValues.Default);
    [Fact] void should_preserve_a_named_tenant() => _values["North"].ShouldEqual("North");
    [Fact] void should_preserve_named_tenant_casing() => _values["default"].ShouldEqual("default");
    [Fact] void should_reject_collisions() => _errors.TrueForAll(error => error is AmbiguousTenant).ShouldBeTrue();
    [Fact] void should_not_append_for_an_ambiguous_tenant() => _appended.ShouldEqual(_appendedBeforeCollisions);
}
