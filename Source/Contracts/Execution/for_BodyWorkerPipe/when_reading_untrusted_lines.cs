// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerPipe;

public class when_reading_untrusted_lines : Specification
{
    [Theory]
    [InlineData("oversized\n", 4)]
    [InlineData("truncated", 100)]
    [InlineData("ææ\n", 3)]
    async Task should_reject_a_line_outside_the_transport_contract(string text, int bytes)
    {
        using var reader = new StringReader(text);
        var error = await Catch.Exception(async () => await BodyWorkerPipe.ReadLineAsync(reader, bytes, CancellationToken.None));
        error.ShouldBeOfExactType<IOException>();
    }
}
