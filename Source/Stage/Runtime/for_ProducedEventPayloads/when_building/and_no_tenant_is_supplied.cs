// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Runtime.for_ProducedEventPayloads.when_building;

public class and_no_tenant_is_supplied : Specification
{
    IReadOnlyList<ProducedEventPayload> _events = null!;

    void Because() => _events = ProducedEventPayloads.Build(
        [new("Registered", null, [new("tenant", ProducedValueKind.Tenant, string.Empty)], [])],
        new Dictionary<string, System.Text.Json.JsonElement>(),
        DateTimeOffset.UnixEpoch,
        new Dictionary<string, string>());

    [Fact] void should_use_the_portable_default_tenant() => _events[0].Content["tenant"]!.GetValue<string>().ShouldEqual(PortableTenantValues.Default);
}
