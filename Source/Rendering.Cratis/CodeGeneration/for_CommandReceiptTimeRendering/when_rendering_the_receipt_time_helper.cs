// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CommandReceiptTimeRendering;

public class when_rendering_the_receipt_time_helper : Specification
{
    string _content = null!;

    void Because() => _content = CommandReceiptTimeRendering.Render("Billing").Content;

    [Fact] void should_use_the_arc_operation_accessor() => _content.ShouldContain("OccurredAtReceipt(global::Cratis.Arc.IOperationContextAccessor operation)");
    [Fact] void should_read_the_dispatch_receipt_once() => _content.Split("operation.ReceivedAt", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_refuse_a_missing_receipt() => _content.ShouldContain("var receivedAt = operation.ReceivedAt ?? throw new CommandReceiptTimeUnavailable();");
    [Fact] void should_refuse_an_unset_receipt() => _content.ReplaceLineEndings("\n").ShouldContain("if (receivedAt == default)\n        {\n            throw new CommandReceiptTimeUnavailable();\n        }");
    [Fact] void should_normalize_to_utc_milliseconds() => _content.ShouldContain("return global::System.DateTimeOffset.FromUnixTimeMilliseconds(receivedAt.ToUnixTimeMilliseconds());");
    [Fact] void should_preserve_the_missing_receipt_message() => _content.ShouldContain("The command requires an active Arc operation with a receipt time.");
    [Fact] void should_not_read_wall_clock_time() => _content.ShouldNotContain("UtcNow");
    [Fact] void should_not_introduce_another_time_provider() => _content.ShouldNotContain("TimeProvider");
    [Fact] void should_not_read_local_wall_clock_time() => _content.ShouldNotContain(".Now");
}
