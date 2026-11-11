// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_SignalContractTriggers;

public class when_nothing_is_routed : Specification
{
    IReadOnlyCollection<string> _reactions = null!;

    void Because() => _reactions = new NoContractTriggerRoutes().WorkflowsFor("Slack", "MessageReceived");

    [Fact] void should_react_to_nothing() => _reactions.ShouldBeEmpty();
}
