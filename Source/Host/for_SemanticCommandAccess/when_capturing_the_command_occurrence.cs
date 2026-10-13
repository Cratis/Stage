// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticCommandAccess;

public class when_capturing_the_command_occurrence : Specification
{
    readonly DateTimeOffset _receipt = new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);
    CommandContext _context = null!;
    SemanticCommandOccurrence _occurrence = null!;

    void Establish() => _context = new(CorrelationId.New(), typeof(object), new object(), [], []) { ReceivedAt = _receipt };
    void Because() => _occurrence = SemanticCommandAccess.Occurrence(_context, new ClaimsPrincipal());

    [Fact] void should_use_the_dispatch_receipt_time() => _occurrence.Occurred.ShouldEqual(_receipt);
    [Fact] void should_reuse_the_same_occurrence() => SemanticCommandAccess.Occurrence(_context, new ClaimsPrincipal()).ShouldBeSame(_occurrence);
}
