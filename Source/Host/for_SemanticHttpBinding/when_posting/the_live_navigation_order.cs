// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class the_live_navigation_order : the_live_navigation_model
{
    int _status;
    string _body = null!;

    async Task Because() => (_status, _body, _) = await Request("POST", "/api/sales/orders", "{\"orderId\":\"11111111-1111-4111-8111-111111111111\",\"customer\":\"Fjordline AS\",\"quantity\":4}");

    [Fact] void should_accept_the_command() => _status.ShouldEqual(200);
    [Fact] void should_not_report_an_unsupported_identity_allocation() => _body.ShouldNotContain("IdentityAllocation");
    [Fact] async Task should_append_the_placed_order() => await _facts.Received(1).Append(Arg.Is<IReadOnlyList<SemanticFact>>(facts => facts.Count == 1), Arg.Any<SemanticCommandOccurrence>());
}
