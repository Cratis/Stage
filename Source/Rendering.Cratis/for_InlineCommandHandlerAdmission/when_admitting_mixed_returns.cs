// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_InlineCommandHandlerAdmission;

public class when_admitting_mixed_returns
{
    [Theory]
    [InlineData("if (done) return; return new OrderPlaced();")]
    [InlineData("if (done) return new OrderPlaced(); return;")]
    [InlineData("if (done) return; return events;")]
    public void should_reject_the_body_and_explain_how_to_fix_it(string body)
    {
        var location = SourceLocation.Start with { Path = "authored/Handlers.play", Line = 31, Column = 13 };
        var code = new CodeBlockSyntax("csharp", body, location);
        var error = Catch.Exception(() => InlineCommandHandlerAdmission.EnsureAccepted(code, "ProcessBatch", "Billing.Invoices.ProcessBatch"));

        error.ShouldBeOfExactType<UnsupportedInlineCommandHandler>();
        var rejection = (UnsupportedInlineCommandHandler)error;
        rejection.Reason.ShouldEqual(InlineCommandHandlerRejectionReason.MixedReturns);
        rejection.Location.ShouldEqual(location);
        rejection.Message.ShouldEqual(
            "STAGE-CRATIS-INLINE-001: Command 'ProcessBatch' in slice 'Billing.Invoices.ProcessBatch' at authored/Handlers.play(31,13) " +
            "has an unsupported inline handler: MixedReturns. The handler mixes 'return;' with returning a value. Return a value or nothing on every path.");
    }
}
